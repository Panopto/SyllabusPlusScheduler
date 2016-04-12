using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace ScheduleRecordingServices
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {

            // Make sure when releasing the exe and msi, to build using "Release"
#if DEBUG
            ScheduleRecordingService scheduleRecordingService = new ScheduleRecordingService();
            scheduleRecordingService.OnDebug();
#else
            ServiceBase[] servicesToRun;
            servicesToRun = new ServiceBase[] 
            { 
                new ScheduleRecordingService() 
            };
            ServiceBase.Run(servicesToRun);
#endif
        }
    }
}
