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
            log.Debug("Attempting sync at: " + DateTime.Now.ToString());
            if (!isRunning)
            {
                lock (thisLock)
                {
                    this.isRunning = true;
                    this.ScheduleRecordings();
                    this.isRunning = false;
                }
            }
            else
            {
                log.Error("Sync could not be started. Previous sync is still running.");
            }
            log.Debug("Sync completed at: " + DateTime.Now.ToString());
        }

        /// <summary>
        /// Makes a SOAP api call to schedule a recording
        /// </summary>
        private void ScheduleRecordings()
        {
            log.Debug("Sync started at: " + DateTime.Now.ToString());
            try
            {
                Schedule schedule = null;
                
                do
                {
                    using (SyllabusPlusDBContext db = new SyllabusPlusDBContext())
                    {
                        schedule =
                            db.SchedulesTable.Select(s => s)
                              .Where(  s => !s.LastPanoptoSync.HasValue
                                         || (   s.LastUpdate > s.LastPanoptoSync.Value
                                             && s.PanoptoSyncSuccess == true)
                                         || s.PanoptoSyncSuccess == null
                                         || (   s.PanoptoSyncSuccess == false
                                             && s.NumberOfAttempts < MAX_ATTEMPTS))
                              .OrderBy(s => s.LastUpdate).FirstOrDefault();

                        try
                        {
                            if (schedule != null)
                            {
                                log.Debug(
                                    String.Format("Attempting to sync a session. Name: {0}, Last update: {1}, Last Panopto sync: {2}, Panopto sync success: {3}, Number of attempts: {4}",
                                                  schedule.SessionName,
                                                  schedule.LastUpdate,
                                                  (schedule.LastPanoptoSync != null) ? (DateTime?) schedule.LastPanoptoSync.Value : null,
                                                  schedule.PanoptoSyncSuccess,
                                                  schedule.NumberOfAttempts));
                                if (!schedule.CancelSchedule.HasValue)
                                {
                                    ScheduledRecordingResult result = null;

                                    using (RemoteRecorderManagementWrapper remoteRecorderManagementWrapper
                                        = new RemoteRecorderManagementWrapper(
                                            this.configSettings.PanoptoSite,
                                            this.configSettings.PanoptoUserName,
                                            this.configSettings.PanoptoPassword))
                                    {
                                        // Schedule session id will determine if need to create or update/delete the corresponding schedule
                                        if (schedule.ScheduledSessionId == null || schedule.ScheduledSessionId == Guid.Empty)
                                        {
                                            log.Debug(schedule.SessionName + " is not associated with a session on the Panopto database. Attempting to schedule.");
                                            result = remoteRecorderManagementWrapper.ScheduleRecording(schedule);
                                        }
                                        else
                                        {
                                            log.Debug(schedule.SessionName + " is already associated with a session on the Panopto database. Attempting to update schedule.");
                                            result = remoteRecorderManagementWrapper.UpdateRecordingTime(schedule);
                                        }
                                    }

                                    schedule.PanoptoSyncSuccess = !result.ConflictsExist;

                                    if (!result.ConflictsExist)
                                    {
                                        log.Debug(schedule.SessionName + " sync succeeded.");
                                        // Should only be 1 valid Session ID and never null
                                        schedule.ScheduledSessionId = result.SessionIDs.FirstOrDefault();
                                        schedule.NumberOfAttempts = 0;
                                        schedule.ErrorResponse = null;

                                        if (schedule.LastUpdate > DateTime.UtcNow)
                                        {
                                            // In the rare case that the LastUpdateTime was in the future, set it to now, to ensure we don't repeat sync
                                            schedule.LastUpdate = DateTime.UtcNow;
                                        }
                                    }
                                    else
                                    {
                                        schedule.ErrorResponse = this.xmlScheduledRecordingHelper.SerializeXMLToString(result);
                                        schedule.NumberOfAttempts++;
                                        if (schedule.NumberOfAttempts >= MAX_ATTEMPTS)
                                        {
                                            log.Error(schedule.SessionName + " failed to sync.");
                                        }
                                    }
                                }

                                // Cancel Schedule has been requested and not succeeded
                                else if (schedule.CancelSchedule == false)
                                {
                                    log.Debug("Cancelling " + schedule.SessionName);
                                    using (SessionManagementWrapper sessionManagementWrapper
                                        = new SessionManagementWrapper(
                                            this.configSettings.PanoptoSite,
                                            this.configSettings.PanoptoUserName,
                                            this.configSettings.PanoptoPassword))
                                    {
                                        sessionManagementWrapper.DeleteSessions((Guid)schedule.ScheduledSessionId);
                                        schedule.CancelSchedule = true;
                                        schedule.PanoptoSyncSuccess = true;
                                        schedule.NumberOfAttempts = 0;
                                        schedule.ErrorResponse = null;
                                    }
                                }

                                schedule.LastPanoptoSync = DateTime.UtcNow;
                            }
                        }
                        catch (Exception ex)
                        {
                            log.Error("Error syncing schedule " + ex.ToString(), ex);
                            schedule.ErrorResponse = ex.Message;
                            schedule.PanoptoSyncSuccess = false;
                            schedule.NumberOfAttempts++;
                        }

                        try
                        {
                            if (schedule != null)
                            {
                                log.Debug(
                                    String.Format("Attempting to save schedule back to database. Name: {0}, Last update: {1}, Last Panopto sync: {2}, Panopto sync success: {3}, Number of attempts: {4}",
                                                  schedule.SessionName,
                                                  schedule.LastUpdate,
                                                  (schedule.LastPanoptoSync != null) ? (DateTime?)schedule.LastPanoptoSync.Value : null,
                                                  schedule.PanoptoSyncSuccess,
                                                  schedule.NumberOfAttempts));

                                // Save after every iteration to prevent scheduling not being insync with Panopto Server
                                db.SaveChanges();
                            }
                        }
                        catch (Exception ex)
                        {
                            log.Error("Error saving to database " + ex.ToString(), ex);
                        }
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
