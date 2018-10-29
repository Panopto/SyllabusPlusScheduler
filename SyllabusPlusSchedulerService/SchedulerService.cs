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
using SyllabusPlusSchedulerService.SessionManagement;
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
            // Attempt to run a single sync for debug purposes.
            this.ScheduleRecordings();
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
        /// Verify the folder exists before scheduling a recording. If not, put in RR's default folder (just set folder GUID to empty).
        /// </summary>
        private ScheduledRecordingResult VerifyFolderAndScheduleRecording(SessionManagementWrapper sessionManagementWrapper, Schedule schedule, RemoteRecorderManagementWrapper remoteRecorderManagementWrapper)
        {
            // Verify the folder exists before trying to schedule a recording
            try
            {
                sessionManagementWrapper.GetFolderById(schedule.FolderId);
            }
            catch (Exception e)
            {
                // If folder with specified ID can't be found, set folder ID to Guid.Empty to create the recording in the Remote Recorder default folder.
                log.Error(String.Format("Can't find folder with ID {0} in the Panopto database.", schedule.FolderId), e);
                log.Warn(String.Format("Recording will be scheduled in folder with ID {0}.", this.configSettings.PanoptoDefaultFolder));
                schedule.FolderId = this.configSettings.PanoptoDefaultFolder;
            }
            return remoteRecorderManagementWrapper.ScheduleRecording(schedule);
        }

        private bool CheckUsernameAndUpdateOwner(UserManagementWrapper userManagementWrapper, SessionManagementWrapper sessionManagementWrapper ,string Username, Guid sessionid)
        {
          
               Guid userid =  userManagementWrapper.GetUserIdByName(Username);
            log.Warn($"{userid}");
            if (userid == null)
            {
                log.Error(String.Format("Can't find user named {0} in the Panopto database.", Username));
                log.Warn(String.Format("Recording will be scheduled with the service account as the owner: {0}.", this.configSettings.PanoptoUserName));
                return false;
            }
            else
            {
                return sessionManagementWrapper.UpdateSessionOwner(sessionid, Username);
            }
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
                                .Where(s => (s.NumberOfAttempts < MAX_ATTEMPTS 
                                            && (!s.LastPanoptoSync.HasValue
                                                || s.LastUpdate > s.LastPanoptoSync.Value
                                                || !s.PanoptoSyncSuccess.HasValue)))
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
                                    using (SessionManagementWrapper sessionManagementWrapper
                                        = new SessionManagementWrapper(
                                        this.configSettings.PanoptoSite,
                                        this.configSettings.PanoptoUserName,
                                        this.configSettings.PanoptoPassword))
                                    using (UserManagementWrapper userManagementWrapper
                                        = new UserManagementWrapper(
                                        this.configSettings.PanoptoSite,
                                        this.configSettings.PanoptoUserName,
                                        this.configSettings.PanoptoPassword))


                                    {
                                        // Schedule session id will determine if need to create or update/delete the corresponding schedule
                                        if (schedule.ScheduledSessionId == null || schedule.ScheduledSessionId == Guid.Empty)
                                        {
                                            log.Debug(schedule.SessionName + " is not associated with a session on the Panopto database. Attempting to schedule.");
                                            result = VerifyFolderAndScheduleRecording(sessionManagementWrapper, schedule, remoteRecorderManagementWrapper);

                                        }
                                        else
                                        {
                                            log.Debug(schedule.SessionName + " is already associated with a session on the Panopto database. Attempting to update schedule.");
                                            Session scheduledSession = sessionManagementWrapper.GetSessionById((Guid)schedule.ScheduledSessionId);

                                            // Check if either the primary or secondary remote recorder changed. If so, delete the old session and create a new session.
                                            if (schedule.PrimaryRemoteRecorderId != scheduledSession.RemoteRecorderIds[0]
                                                || (scheduledSession.RemoteRecorderIds.Length > 1 && schedule.SecondaryRemoteRecorderId != scheduledSession.RemoteRecorderIds[1]))
                                            {
                                                log.Debug(schedule.SessionName + " has a different remote recorder. Moving the recording by deleting the existing scheduled session and creating a new scheduled session.");
                                                sessionManagementWrapper.DeleteSessions((Guid)schedule.ScheduledSessionId);
                                                // Reset the ScheduledSessionId in the DB just in case rescheduling the session fails the first time.
                                                schedule.ScheduledSessionId = null;
                                                result = VerifyFolderAndScheduleRecording(sessionManagementWrapper, schedule, remoteRecorderManagementWrapper);
                                            }
                                            else
                                            {
                                                // Check if name was updated in DB. If so, update the session name on the server.
                                                if (scheduledSession.Name != schedule.SessionName)
                                                {
                                                    log.Debug(String.Format("Updating the session name from {0} to {1}.", scheduledSession.Name, schedule.SessionName));
                                                    sessionManagementWrapper.UpdateSessionName((Guid)schedule.ScheduledSessionId, schedule.SessionName);
                                                }
                                                // Check if start time was updated in DB. If so, update the start time on the server. Could fail if there is a conflict.
                                                if (scheduledSession.StartTime != schedule.StartTime)
                                                {
                                                    log.Debug("Updating the scheduled session's start time.");
                                                    result = remoteRecorderManagementWrapper.UpdateRecordingTime(schedule);
                                                }

                                                if (scheduledSession.Duration != Convert.ToDouble(schedule.Duration)*60)
                                                {
                                                    log.Debug("Updating the scheduled duration.");
                                                    log.Debug($"Old duration in seconds {scheduledSession.Duration}, New duration in seconds {Convert.ToDouble(schedule.Duration)}*60");
                                                    result = remoteRecorderManagementWrapper.UpdateRecordingTime(schedule);
                                                }
                                                if (schedule.PresenterUsername != null)
                                                {
                                                    log.Debug("Updating the session owner.");
                                                    bool Username =   CheckUsernameAndUpdateOwner(userManagementWrapper, sessionManagementWrapper, schedule.PresenterUsername, (Guid)schedule.ScheduledSessionId);
                                                    if (Username == true)
                                                    { log.Debug(schedule.SessionName + " has had owner updated to " + schedule.PresenterUsername); }
                                                    else
                                                    { schedule.ErrorResponse = "Owner update failed. Username:" + schedule.PresenterUsername + "could not be located"; }
                                                }
                                            }
                                        }
                                    }

                                    // If just updating the session name, the ScheduleRecordingResult object will be null.
                                    schedule.PanoptoSyncSuccess = result != null ? !result.ConflictsExist : true;
                                    using (UserManagementWrapper userManagementWrapper
                                   = new UserManagementWrapper(
                                   this.configSettings.PanoptoSite,
                                   this.configSettings.PanoptoUserName,
                                   this.configSettings.PanoptoPassword))

                                    using (SessionManagementWrapper sessionManagementWrapper
                                   = new SessionManagementWrapper(
                                   this.configSettings.PanoptoSite,
                                   this.configSettings.PanoptoUserName,
                                   this.configSettings.PanoptoPassword))

                                        if ((bool)schedule.PanoptoSyncSuccess)
                                        {
                                            log.Debug(schedule.SessionName + " sync succeeded.");
                                            // Should only be 1 valid Session ID. ScheduleRecordingResult could be null if just updating the session name.
                                            if (result != null)
                                            {
                                                schedule.ScheduledSessionId = result.SessionIDs.FirstOrDefault();
                                                if (schedule.PresenterUsername != null)
                                                {
                                                   bool Username = CheckUsernameAndUpdateOwner(userManagementWrapper, sessionManagementWrapper, schedule.PresenterUsername, schedule.ScheduledSessionId.Value);
                                                    if (Username == true)
                                                    { log.Debug(schedule.SessionName + " has had owner updated to " + schedule.PresenterUsername); }
                                                    else
                                                    { schedule.ErrorResponse = "Owner update failed. Username:" + schedule.PresenterUsername + "could not be located"; }
                                                }
                                            }
                                            schedule.NumberOfAttempts = 0;
                                            

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
