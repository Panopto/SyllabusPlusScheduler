using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using SyllabusPlusSchedulerService.SessionManagement;
using SyllabusPlusSchedulerService.Utility;

namespace SyllabusPlusSchedulerService.PublicApiWrapper
{
    internal class SessionManagementWrapper : IDisposable
    {
        /// <summary>
        /// Session Manager Client to call Soap API
        /// </summary>
        SessionManagementClient sessionManagement;

        /// <summary>
        /// Authentication Info
        /// </summary>
        AuthenticationInfo authentication;

        /// <summary>
        /// Constructor to create a SessionManagement including
        /// necessary information to create the SOAP API calls
        /// </summary>
        /// <param name="configSettings">App settings used for API request</param>
        public SessionManagementWrapper(ConfigSettings configSettings)
        {
            // Update Service endpoint to reflect specified server name
            UriBuilder sessionManagementUriBuilder = new UriBuilder();
            sessionManagementUriBuilder.Scheme = "https";
            sessionManagementUriBuilder.Host = configSettings.PanoptoSite;
            sessionManagementUriBuilder.Path = @"Panopto/PublicAPI/4.6/SessionManagement.svc";

            this.sessionManagement = new SessionManagementClient(
                                    new BasicHttpBinding(BasicHttpSecurityMode.Transport)
                                    {
                                        MaxReceivedMessageSize = 10000000,
                                        SendTimeout = TimeSpan.FromMinutes(10),
                                        ReceiveTimeout = TimeSpan.FromMinutes(10)
                                    },
                                    new EndpointAddress(sessionManagementUriBuilder.Uri));

            this.authentication = new AuthenticationInfo()
            {
                UserKey = configSettings.PanoptoUserName,
                Password = configSettings.PanoptoPassword
            };
        }

        /// <summary>
        /// Deletes sessions. WARNING: permanently deletes data. Must be called by a videographer, creator or admin
        /// </summary>
        /// <param name="sessionId">Session to delete</param>
        public void DeleteSessions(Guid sessionId)
        {
            this.sessionManagement.DeleteSessions(this.authentication, new Guid[] { sessionId });
        }

        /// <summary>
        /// Clean up object
        /// </summary>
        public void Dispose()
        {
            if (this.sessionManagement.State == CommunicationState.Faulted)
            {
                this.sessionManagement.Abort();
            }

            if (this.sessionManagement.State != CommunicationState.Closed)
            {
                this.sessionManagement.Close();
            }
        }
    }
}
