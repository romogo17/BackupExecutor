using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace BackupExecutor
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            if (Environment.UserInteractive)
            {
                BackupExecutorService service1 = new BackupExecutorService(args);
                service1.TestStartupAndStop(args);
            }
            else
            {
                ServiceBase[] ServicesToRun = new ServiceBase[] { new BackupExecutorService(args) };
                ServiceBase.Run(ServicesToRun);
            }
        }
    }
}
