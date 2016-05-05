using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace SyllabusPlusSchedulerService
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
            SchedulerService schedulerService = new SchedulerService();
            schedulerService.OnDebug();
#else
            ServiceBase[] servicesToRun;
            servicesToRun = new ServiceBase[] 
            { 
                new SchedulerService() 
            };
            ServiceBase.Run(servicesToRun);
#endif
        }
    }
}
