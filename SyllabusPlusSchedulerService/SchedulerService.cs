using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using SyllabusPlusSchedulerService.RemoteRecorderManagement;
using SyllabusPlusSchedulerService.Log;
using SyllabusPlusSchedulerService.DB;
using SyllabusPlusSchedulerService.Utility;
using SyllabusPlusSchedulerService.PublicApiWrapper;

namespace SyllabusPlusSchedulerService
{
    internal partial class SchedulerService : ServiceBase
    {
        private static readonly EventLogger log = new EventLogger();
        private Timer Scheduler;
        private const int MAX_ATTEMPTS = 3;
        private bool isRunning;
        private object thisLock = new object();
        private ConfigSettings configSettings = new ConfigSettings();
        private XmlHelper<ScheduledRecordingResult> xmlScheduledRecordingHelper = new XmlHelper<ScheduledRecordingResult>();

        public SchedulerService()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Used only for Debugging without installing the service
        /// </summary>
        public void OnDebug()
        {
            this.OnStart(null);
        }

        /// <summary>
        /// Service Method when Service first starts
        /// </summary>
        /// <param name="args"></param>
        protected override void OnStart(string[] args)
        {
            log.Debug("Syllabus Plus Scheduler Service started.");
            this.Scheduler = new Timer(new TimerCallback(ScheduleCallback),
                state: null, dueTime: 0, period: (this.configSettings.SyncIntervalInMinutes * 60000));
        }

        /// <summary>
        /// Service method when service stops
        /// </summary>
        protected override void OnStop()
        {
            log.Debug("Syllabus Plus Scheduler Service stopped.");
            this.Scheduler.Dispose();
        }

        /// <summary>
        /// Callback method to check in and schedule
        /// recordings if applicable
        /// </summary>
        /// <param name="e"></param>
        private void ScheduleCallback(object e)
        {
            if (!isRunning)
            {
                lock (thisLock)
                {
                    this.isRunning = true;
                    this.ScheduleRecordings();
                    this.isRunning = false;
                }
            }
        }

        /// <summary>
        /// Makes a SOAP api call to schedule a recording
        /// </summary>
        private void ScheduleRecordings()
        {
            try
            {
                Schedule schedule = null;
                
                do
                {
                    using (SyllabusPlusDBContext db = new SyllabusPlusDBContext())
                    {
                        schedule =
                            db.SchedulesTable.Select(s => s).
                                Where(  s => (s.lastUpdate > s.lastPanoptoSync && s.panoptoSyncSuccess == true)
                                    ||  s.panoptoSyncSuccess == null
                                    || (s.panoptoSyncSuccess == false && s.numberOfAttempts < MAX_ATTEMPTS)).
                                OrderBy(s => s.lastUpdate).FirstOrDefault();

                        try
                        {
                            if (schedule != null)
                            {
                                if (!schedule.cancelSchedule.HasValue)
                                {
                                    ScheduledRecordingResult result = null;

                                    using (RemoteRecorderManagementWrapper remoteRecorderManagementWrapper
                                        = new RemoteRecorderManagementWrapper(
                                            this.configSettings.PanoptoSite,
                                            this.configSettings.PanoptoUserName,
                                            this.configSettings.PanoptoPassword))
                                    {
                                        // Schedule session id will determine if need to create or update/delete the corresponding schedule
                                        if (schedule.scheduledSessionID == null || schedule.scheduledSessionID == Guid.Empty)
                                        {
                                            result = remoteRecorderManagementWrapper.ScheduleRecording(schedule);
                                        }
                                        else
                                        {
                                            result = remoteRecorderManagementWrapper.UpdateRecordingTime(schedule);
                                        }
                                    }

                                    schedule.panoptoSyncSuccess = !result.ConflictsExist;

                                    if (!result.ConflictsExist)
                                    {
                                        // Should only be 1 valid Session ID and never null
                                        schedule.scheduledSessionID = result.SessionIDs.FirstOrDefault();
                                        schedule.numberOfAttempts = 0;
                                    }
                                    else
                                    {
                                        schedule.errorResponse = this.xmlScheduledRecordingHelper.SerializeXMLToString(result);
                                        schedule.numberOfAttempts++;
                                    }
                                }

                                // Cancel Schedule has been requested and not succeeded
                                else if (schedule.cancelSchedule == false)
                                {
                                    using (SessionManagementWrapper sessionManagementWrapper
                                        = new SessionManagementWrapper(
                                            this.configSettings.PanoptoSite,
                                            this.configSettings.PanoptoUserName,
                                            this.configSettings.PanoptoPassword))
                                    {
                                        sessionManagementWrapper.DeleteSessions((Guid)schedule.scheduledSessionID);
                                        schedule.cancelSchedule = true;
                                        schedule.panoptoSyncSuccess = true;
                                        schedule.numberOfAttempts = 0;
                                    }
                                }

                                schedule.lastPanoptoSync = DateTime.UtcNow;
                            }
                        }
                        catch (Exception ex)
                        {
                            log.Error(ex.Message, ex);
                            schedule.errorResponse = ex.Message;
                            schedule.panoptoSyncSuccess = false;
                            schedule.numberOfAttempts++;
                        }

                        // Save after every iteration to prevent scheduling not being insync with Panopto Server
                        db.SaveChanges();
                    }
                } while (schedule != null);
            }
            catch (Exception ex)
            {
                log.Error(ex.Message, ex);
            }
        }
    }
}
