using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScheduleRecordingServices
{
    internal class Schedule
    {
        /// <summary>
        /// Primary Key
        /// </summary>
        public int ID { get; set; }

        /// <summary>
        /// Name of the Recording to be shown in the Panopto UI
        /// </summary>
        public string sessionName { get; set; }

        /// <summary>
        /// Panopto Folder Public Id
        /// </summary>
        public Guid folderID { get; set; }

        /// <summary>
        /// Primary Remote Recorder Public Id
        /// </summary>
        public Guid primaryRemoteRecorderID { get; set; }

        /// <summary>
        /// Secondary Remote Recorder Public Id
        /// </summary>
        public Guid? secondaryRemoteRecorderID { get; set; }

        /// <summary>
        /// Time in UTC to start the recording
        /// </summary>
        public DateTime startTime { get; set; }

        /// <summary>
        /// Duration in minutes
        /// </summary>
        public int duration { get; set; }

        /// <summary>
        /// Presenter's Panopto Username.
        /// If no session creator is provided, 
        /// use remote recorder service as the creator
        /// </summary>
        public string presenterUsername { get; set; }

        /// <summary>
        /// Cancel Schedule
        /// null = not canceled
        /// 0 = cancel requested
        /// 1 = cancel completed
        /// </summary>
        public bool? cancelSchedule { get; set; }

        /// <summary>
        /// Determine if remote recorder is a webcast
        /// </summary>
        public bool webcast { get; set; }

        /// <summary>
        /// Scheduled Recording Public Id
        /// Id initially starts off as null 
        /// and is populated by the scheduler tool
        /// </summary>
        public Guid? scheduledSessionID { get; set; }

        /// <summary>
        /// Time row was last updated in UTC
        /// </summary>
        public DateTime lastUpdate { get; set; }

        /// <summary>
        /// Last time this row was sync'd to Panopto
        /// </summary>
        public DateTime? lastPanoptoSync { get; set; }

        /// <summary>
        /// Nullable bool to determine if Sync was successful
        /// </summary>
        public bool? panoptoSyncSuccess { get; set; }

        /// <summary>
        /// Serialized response if there were conflicting
        /// sessions or any other issue that we can log
        /// to indicate why the session scheduling failed
        /// </summary>
        public string errorResponse { get; set; }
    }
}
