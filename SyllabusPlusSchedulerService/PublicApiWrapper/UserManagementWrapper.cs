using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using SyllabusPlusSchedulerService.UserManagement;
namespace SyllabusPlusSchedulerService.PublicApiWrapper
{
    internal class UserManagementWrapper : IDisposable
    {
        /// <summary>
        /// Session Manager Client to call Soap API
        /// </summary>
        UserManagementClient userManagement;

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
        public UserManagementWrapper(string site, string username, string password)
        {
            // Update Service endpoint to reflect specified server name
            UriBuilder userManagementUriBuilder = new UriBuilder();
            userManagementUriBuilder.Scheme = "https";
            userManagementUriBuilder.Host = site;
            userManagementUriBuilder.Path = @"Panopto/PublicAPI/4.6/UserManagement.svc";

            this.userManagement = new UserManagementClient(
                                    new BasicHttpBinding(BasicHttpSecurityMode.Transport)
                                    {
                                        MaxReceivedMessageSize = 10000000,
                                        SendTimeout = TimeSpan.FromMinutes(10),
                                        ReceiveTimeout = TimeSpan.FromMinutes(10)
                                    },
                                    new EndpointAddress(userManagementUriBuilder.Uri));

            this.authentication = new AuthenticationInfo()
            {
                UserKey = username,
                Password = password
            };
        }


        /// <summary>
        /// Gets a user ID by username
        /// </summary>
        /// <param name="username">UserName</param>
        public Guid? GetUserIdByName(string username)
        {
            try
            {
                return this.userManagement.GetUserByKey(this.authentication, username).UserId;
            }
            catch (Exception ex)
            { return null; }
        }

         public string GetUserNameByID(Guid UserId)
        {
            return this.userManagement.GetUsers(this.authentication,new Guid [] {UserId}).FirstOrDefault().UserKey;
        }

        /// <summary>
        /// Clean up object
        /// </summary>
        public void Dispose()
        {
            if (this.userManagement.State == CommunicationState.Faulted)
            {
                this.userManagement.Abort();
            }

            if (this.userManagement.State != CommunicationState.Closed)
            {
                this.userManagement.Close();
            }
        }
    }
}
