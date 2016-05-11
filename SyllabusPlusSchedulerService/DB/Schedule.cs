using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyllabusPlusSchedulerService.DB
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
        [Column("sessionName")]
        public string SessionName { get; set; }

        /// <summary>
        /// Panopto Folder Public Id
        /// </summary>
        [Column("folderID")]
        public Guid FolderId { get; set; }

        /// <summary>
        /// Primary Remote Recorder Public Id
        /// </summary>
        [Column("primaryRemoteRecorderID")]
        public Guid PrimaryRemoteRecorderId { get; set; }

        /// <summary>
        /// Secondary Remote Recorder Public Id
        /// </summary>
        [Column("secondaryRemoteRecorderID")]
        public Guid? SecondaryRemoteRecorderId { get; set; }

        /// <summary>
        /// Time in UTC to start the recording
        /// </summary>
        [Column("startTime")]
        public DateTime StartTime { get; set; }

        /// <summary>
        /// Duration in minutes
        /// </summary>
        [Column("duration")]
        public int Duration { get; set; }

        /// <summary>
        /// Presenter's Panopto Username.
        /// If no session creator is provided, 
        /// use remote recorder service as the creator
        /// </summary>
        [Column("presenterUsername")]
        public string PresenterUsername { get; set; }

        /// <summary>
        /// Cancel Schedule
        /// null = not canceled
        /// 0 = cancel requested
        /// 1 = cancel completed
        /// </summary>
        [Column("cancelSchedule")]
        public bool? CancelSchedule { get; set; }

        /// <summary>
        /// Determine if remote recorder is a webcast
        /// </summary>
        [Column("webcast")]
        public bool Webcast { get; set; }

        /// <summary>
        /// Scheduled Recording Public Id
        /// Id initially starts off as null 
        /// and is populated by the scheduler tool
        /// </summary>
        [Column("scheduledSessionID")]
        public Guid? ScheduledSessionId { get; set; }

        /// <summary>
        /// Time row was last updated in UTC
        /// </summary>
        [Column("lastUpdate")]
        public DateTime LastUpdate { get; set; }

        /// <summary>
        /// Last time this row was sync'd to Panopto
        /// </summary>
        [Column("lastPanoptoSync")]
        public DateTime? LastPanoptoSync { get; set; }

        /// <summary>
        /// Nullable bool to determine if Sync was successful
        /// </summary>
        [Column("panoptoSyncSuccess")]
        public bool? PanoptoSyncSuccess { get; set; }

        /// <summary>
        /// Number of attempts of either creating, scheduling, or deleting schedule
        /// </summary>
        [Column("numberOfAttempts")]
        public int NumberOfAttempts { get; set; }

        /// <summary>
        /// Serialized response if there were conflicting
        /// sessions or any other issue that we can log
        /// to indicate why the session scheduling failed
        /// </summary>
        [Column("errorResponse")]
        public string ErrorResponse { get; set; }
    }
}
