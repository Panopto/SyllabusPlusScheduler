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
        /// <param name="site">Panopto Site</param>
        /// <param name="username">admin username</param>
        /// <param name="password">password associated with username</param>
        public SessionManagementWrapper(string site, string username, string password)
        {
            // Update Service endpoint to reflect specified server name
            UriBuilder sessionManagementUriBuilder = new UriBuilder();
            sessionManagementUriBuilder.Scheme = "https";
            sessionManagementUriBuilder.Host = site;
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
                UserKey = username,
                Password = password
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
        /// Gets a folder by ID
        /// </summary>
        /// <param name="folderId">Folder ID</param>
        public Folder GetFolderById(Guid folderId)
        {
            return this.sessionManagement.GetFoldersById(this.authentication, new Guid[] { folderId }).First<Folder>();
        }

        /// <summary>
        /// Gets a session by ID
        /// </summary>
        /// <param name="sessionId">Session ID</param>
        public Session GetSessionById(Guid sessionId)
        {
            return this.sessionManagement.GetSessionsById(this.authentication, new Guid[] { sessionId }).First<Session>();
        }

        /// <summary>
        /// Updates the session name. No-op if the Guid is null.
        /// </summary>
        /// <param name="sessionId">Session to update</param>
        /// <param name="name">New name for the session</param>
        public void UpdateSessionName(Guid sessionId, string name)
        {
            this.sessionManagement.UpdateSessionName(this.authentication, sessionId, name);
        }

        public bool UpdateSessionOwner (Guid sessionId, string username)
        {
            try
            {
                this.sessionManagement.UpdateSessionOwner(this.authentication, new Guid[] { sessionId }, username);
                return true;
            }
            catch (Exception)
            {
                return false;
            }

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
