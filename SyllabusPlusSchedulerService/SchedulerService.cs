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
            this.ScheduleRecordings();
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
            this.ScheduleRecordings();
        }

        /// <summary>
        /// Makes a SOAP api call to schedule a recording
        /// </summary>
        private void ScheduleRecordings()
        {
            try
            {
                this.Scheduler = new Timer(new TimerCallback(ScheduleCallback));

                // Gets and Sets App Settings
                ConfigSettings configSettings = new ConfigSettings();

                using (RemoteRecorderManagementWrapper remoteRecorderManagementWrapper
                    = new RemoteRecorderManagementWrapper(configSettings))
                {
                    using (SyllabusPlusDBContext db = new SyllabusPlusDBContext())
                    {
                        // BUGBUG: 37305 Determine if panopyoSyncSuccess = false should be considered for reschedule
                        List<Schedule> schedules =
                            db.Schedules.Select(s => s).
                                Where(s => s.lastUpdate > s.lastPanoptoSync || s.panoptoSyncSuccess == null).ToList();

                        foreach (Schedule schedule in schedules)
                        {
                            try
                            {
                                if (!schedule.cancelSchedule.HasValue)
                                {
                                    ScheduledRecordingResult result = null;

                                    // Schedule session id will determine if need to create or update/delete the corresponding schedule
                                    if (schedule.scheduledSessionID == null || schedule.scheduledSessionID == Guid.Empty)
                                    {
                                        result = remoteRecorderManagementWrapper.ScheduleRecording(schedule);
                                    }
                                    else
                                    {
                                        result = remoteRecorderManagementWrapper.UpdateRecordingTime(schedule);
                                    }

                                    schedule.panoptoSyncSuccess = !result.ConflictsExist;

                                    if (!result.ConflictsExist)
                                    {
                                        // Should only be 1 valid Session ID and never null
                                        schedule.scheduledSessionID = result.SessionIDs.FirstOrDefault();
                                    }
                                    else
                                    {
                                        schedule.errorResponse = XmlHelper.SerializeXMLToString(result);
                                    }
                                }
                                // Cancel Schedule has been requested and not succeeded
                                else if (schedule.cancelSchedule == false)
                                {
                                    using (SessionManagementWrapper sessionManagementWrapper = new SessionManagementWrapper(configSettings))
                                    {
                                        sessionManagementWrapper.DeleteSessions((Guid)schedule.scheduledSessionID);
                                        schedule.cancelSchedule = true;
                                        schedule.panoptoSyncSuccess = true;
                                    }
                                }

                                schedule.lastPanoptoSync = schedule.lastUpdate = DateTime.UtcNow;
                            }
                            catch (Exception ex)
                            {
                                log.Error(ex.Message, ex);
                                schedule.errorResponse = XmlHelper.SerializeXMLToString(ex);
                                schedule.lastPanoptoSync = schedule.lastUpdate = DateTime.UtcNow;
                                schedule.panoptoSyncSuccess = false;
                            }

                            // Save after every iteration to prevent scheduling not being insync with Panopto Server
                            db.SaveChanges();
                        }
                    }
                }
                
                this.Scheduler.Change(configSettings.SyncInterval * 60000, Timeout.Infinite);
            }
            catch (Exception ex)
            {
                log.Error(ex.Message, ex);
            }
        }
    }
}
