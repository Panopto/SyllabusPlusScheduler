using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyllabusPlusSchedulerService.Utility
{
    internal class ConfigSettings
    {
        /// <summary>
        /// Targeted Site 
        /// </summary>
        public string PanoptoSite { get; private set; }

        /// <summary>
        /// Username of a Creator, Videographer, or admin
        /// </summary>
        public string PanoptoUserName { get; private set; }

        /// <summary>
        /// Associated password with username
        /// </summary>
        public string PanoptoPassword { get; private set; }

        /// <summary>
        /// Interval when service runs in minutes
        /// </summary>
        public int SyncInterval { get; private set; }

        /// <summary>
        /// ConfigSettings constructor that gets settings from the config
        /// Note: Exceptions are not handled
        /// </summary>
        public ConfigSettings()
        {
            this.PanoptoSite = ConfigurationManager.AppSettings["PanoptoSite"];
            this.PanoptoUserName = ConfigurationManager.AppSettings["PanoptoUserName"];
            this.PanoptoPassword = ConfigurationManager.AppSettings["PanoptoPassword"];
            this.PopulateSyncInterval();

        }

        /// <summary>
        /// Attempts to populate Sync Interval from App.Config. Defaults to 60 minutes
        /// </summary>
        private void PopulateSyncInterval()
        {
            int syncInterval;
            if (Int32.TryParse(ConfigurationManager.AppSettings["SyncInterval"], out syncInterval))
            {
                this.SyncInterval = syncInterval;
            }
            else
            {
                // Default to 60
                this.SyncInterval = 60;
            }
        }
    }
}
