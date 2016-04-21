﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using SyllabusPlusSchedulerService.DB;
using SyllabusPlusSchedulerService.RemoteRecorderManagement;
using SyllabusPlusSchedulerService.Utility;

namespace SyllabusPlusSchedulerService.PublicApiWrapper
{
    internal class RemoteRecorderManagementWrapper : IDisposable
    {
        /// <summary>
        /// Remote Recorder Manager Client to call Soap API
        /// </summary>
        RemoteRecorderManagementClient remoteRecorderManager;

        /// <summary>
        /// Authentication Info
        /// </summary>
        AuthenticationInfo authentication;

        /// <summary>
        /// Constructor to create a RemoteRecorderManagement including
        /// necessary information to create the SOAP API calls
        /// </summary>
        /// <param name="configSettings">App settings used for API request</param>
        public RemoteRecorderManagementWrapper(ConfigSettings configSettings)
        {
            // Update Service endpoint to reflect specified server name
            UriBuilder userManagementUriBuilder = new UriBuilder();
            userManagementUriBuilder.Scheme = "https";
            userManagementUriBuilder.Host = configSettings.PanoptoSite;
            userManagementUriBuilder.Path = @"Panopto/PublicAPI/4.2/RemoteRecorderManagement.svc";

            this.remoteRecorderManager = new RemoteRecorderManagementClient(
                                    new BasicHttpBinding(BasicHttpSecurityMode.Transport)
                                    {
                                        MaxReceivedMessageSize = 10000000,
                                        SendTimeout = TimeSpan.FromMinutes(10),
                                        ReceiveTimeout = TimeSpan.FromMinutes(10)
                                    },
                                    new EndpointAddress(userManagementUriBuilder.Uri));

            this.authentication = new AuthenticationInfo()
            {
                UserKey = configSettings.PanoptoUserName,
                Password = configSettings.PanoptoPassword
            };
        }

        /// <summary>
        /// Wrapper function to schedule recordings
        /// </summary>
        /// <param name="schedule">Schedule recording to update</param>
        /// <returns>ScheduleRecordingResult containing errors if applicable</returns>
        public ScheduledRecordingResult ScheduleRecording(Schedule schedule)
        {
            List<RecorderSettings> recorderSettings = this.GetRecorderSettings(
                schedule.primaryRemoteRecorderID, 
                schedule.secondaryRemoteRecorderID);

            return this.remoteRecorderManager.ScheduleRecording(
               authentication,
               schedule.sessionName,
               schedule.folderID,
               schedule.webcast,
               schedule.startTime,
               schedule.startTime.AddMinutes(schedule.duration),
               recorderSettings.ToArray());
        }

        /// <summary>
        /// Updates the time of a recording. You may not update a recording that has already finished.
        /// </summary>
        /// <param name="schedule">Schedule recording to update</param>
        /// <returns>ScheduleRecordingResult containing errors if applicable</returns>
        public ScheduledRecordingResult UpdateRecordingTime(Schedule schedule)
        {
            return this.remoteRecorderManager.UpdateRecordingTime(
                this.authentication,
                (Guid)schedule.scheduledSessionID,
                schedule.startTime,
                schedule.startTime.AddMinutes(schedule.duration));
        }

        /// <summary>
        /// Creates a list of Recorder Settings with the given Primary and Secondary Recorder Id
        /// </summary>
        /// <param name="primaryRemoteRecorderId">Primary Remote Recorder Id</param>
        /// <param name="secondaryRemoteRecorderId">Secondary Remote Recorder Id. Can be null if non-existent</param>
        /// <returns>List of Recorder settings</returns>
        private List<RecorderSettings> GetRecorderSettings(
            Guid primaryRemoteRecorderId, 
            Guid? secondaryRemoteRecorderId)
        {
            List<RecorderSettings> recorderSettings = new List<RecorderSettings>();

            // Primary remote recorder Id should not be null.
            // The Schedule Recording API call will/should fail
            if (primaryRemoteRecorderId != Guid.Empty)
            {
                recorderSettings.Add(new RecorderSettings()
                                        {
                                            RecorderId = primaryRemoteRecorderId,
                                            SuppressPrimary = false,
                                            SuppressSecondary = true
                                        }
                                     );
            }

            if (secondaryRemoteRecorderId != null
                && secondaryRemoteRecorderId != Guid.Empty)
            {
                recorderSettings.Add(new RecorderSettings()
                                        {
                                            RecorderId = (Guid)secondaryRemoteRecorderId,
                                            SuppressPrimary = true,
                                            SuppressSecondary = false
                                        }
                                     );
            }

            return recorderSettings;
        }

        /// <summary>
        /// Clean up object
        /// </summary>
        public void Dispose()
        {
            if (this.remoteRecorderManager.State == CommunicationState.Faulted)
            {
                this.remoteRecorderManager.Abort();
            }

            if (this.remoteRecorderManager.State != CommunicationState.Closed)
            {
                this.remoteRecorderManager.Close();
            }
        }
    }
}
