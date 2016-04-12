using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScheduleRecordingServices
{
    internal class EventLogger
    {
        private const string AppName = "ScheduleRecordingService";

        public EventLogger()
        {
            if (!EventLog.SourceExists(AppName))
            {
                EventLog.CreateEventSource(AppName, "Application");
            }
        }

        /// <summary>
        /// Writes message to Event Viewer with Information Flag
        /// </summary>
        /// <param name="text">Message to write to Event Viewer</param>
        public void Debug(string text)
        {
            EventLog.WriteEntry(AppName, text, EventLogEntryType.Information);
        }

        /// <summary>
        /// Writes message to Event Viewer with Warning Flag
        /// </summary>
        /// <param name="text">Message to write to Event Viewer</param>
        public void Warn(string text)
        {
            EventLog.WriteEntry(AppName, text, EventLogEntryType.Warning);
        }

        /// <summary>
        /// Writes message to Event Viewer with Error Flag
        /// </summary>
        /// <param name="text">Message to write to Event Viewer</param>
        public void Error(string text)
        {
            EventLog.WriteEntry(AppName, text, EventLogEntryType.Error);
        }

        /// <summary>
        /// Writes message exception to Event Viewer with Error Flag
        /// </summary>
        /// <param name="text">Message to write to Event Viewer</param>
        public void Error(string text, Exception ex)
        {
            Error(text);
            Error(ex.StackTrace);
        }
    }
}
