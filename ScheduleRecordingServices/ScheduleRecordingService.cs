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
using ScheduleRecordingServices.RemoteRecorderManagement;

namespace ScheduleRecordingServices
{
    internal partial class ScheduleRecordingService : ServiceBase
    {
        private static readonly EventLogger log = new EventLogger();
        private Timer Scheduler;

        public ScheduleRecordingService()
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
            log.Debug("Schedule Recording Service started.");
            this.ScheduleRecordings();
        }

        /// <summary>
        /// Service method when service stops
        /// </summary>
        protected override void OnStop()
        {
            log.Debug("Schedule Recording Service stopped.");
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
                    using (PanoptoDBContext db = new PanoptoDBContext())
                    {
                        // BUGBUG: 37305 Determine if panopyoSyncSuccess = false should be considered for reschedule
                        List<Schedule> schedules =
                            db.Schedules.Select(s => s).
                                Where(s => s.lastUpdate > s.lastPanoptoSync || s.panoptoSyncSuccess == null).ToList();

                        foreach (Schedule schedule in schedules)
                        {
                            try
                            {
                                ScheduledRecordingResult result =
                                    remoteRecorderManagementWrapper.ScheduleRecording(schedule);

                                schedule.lastPanoptoSync = schedule.lastUpdate = DateTime.UtcNow;
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
                            catch (Exception ex)
                            {
                                log.Error(ex.Message, ex);
                                schedule.errorResponse = XmlHelper.SerializeXMLToString(ex);
                                schedule.lastPanoptoSync = schedule.lastUpdate = DateTime.UtcNow;
                                schedule.panoptoSyncSuccess = false;
                            }
                        }
                        
                        db.SaveChanges();
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
