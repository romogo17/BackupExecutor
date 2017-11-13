using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.IO;
using System.Collections;
using System.Threading;

namespace BackupExecutor {
    public partial class BackupExecutorService : ServiceBase {

        private System.Diagnostics.EventLog eventLog;
        private int eventId = 0;
        private string centralIp = "localhost";
        private string centralPort = "1521";
        private ServiceModes mode = ServiceModes.MASTER;
        

        /**
         *  BackupExecutorService Constructor
         *  The service can be configured in either --master or --slave mode
         *  
         *  Modes:  
         *      - Master mode requires no further parameters. Usage is "BackupExecutorService --master"
         *      - Slave mode requires a minimum of one parameter. Usage is "BackupExecutorService --slave <ip address> [<port>]"
         *  
         *  The constructor also configures the event log used by the Service. This is set to "DatabaseBackupLog"
         *  You can view the logs in the Windows Event Log
         *  
         */
        public BackupExecutorService(string[] args) {
            InitializeComponent();
            string eventSourceName = "BackupExecutorService";
            string logName = "DatabaseBackupLog";

            // Decide what to do based on the input parameters
            if (args.Count() > 0) {
                // The service will work in master mode
                if(args[0] == "--master") {
                    Console.WriteLine("Service configured in master mode. " +
                        "\nMake sure the Database Links are set correctly to avoid any issues.");
                }
                // The service will work in slave mode
                else if (args[0] == "--slave") {
                    mode = ServiceModes.SLAVE;
                    Console.WriteLine("Service configured in slave mode. ");

                    if (args.Count() > 2) {
                        centralIp = args[1];
                        centralPort = args[2];
                        Console.WriteLine("Central server IP address set to \"{0}\"", centralIp);
                        Console.WriteLine("Central server TCP port set to \"{0}\"", centralPort);
                    } else if (args.Count() > 1) {
                        centralIp = args[1];
                        Console.WriteLine("Central server IP address set to \"{0}\"", centralIp);
                        Console.WriteLine("Central server TCP port set to deffault \"{0}\"", centralPort);
                    } else {
                        Console.WriteLine("ERROR: Insufficient parameters! Correct use is: \"BackupExecutorService --slave <ip address> [<port>]\"");
                        Console.WriteLine("Service will start in master mode");
                        mode = ServiceModes.MASTER;
                    }
                } 
            } else {
                Console.WriteLine("No suitable parameters in the arguments. Service will start in master mode.");
            }

            // Configure the EventLog used by the service
            eventLog = new System.Diagnostics.EventLog();
            if (!System.Diagnostics.EventLog.SourceExists(eventSourceName)) {
                System.Diagnostics.EventLog.CreateEventSource(eventSourceName, logName);
            }
            eventLog.Source = eventSourceName;
            eventLog.Log = logName;
        }

        /**
         *  OnStart function
         *  Runs when the services is starting
         *  
         *  Checks for required programs such as Oracle.exe and Rman.exe.
         *  Also, checks whether Oracle Services is running or not. If Oracle service is stopped
         *  the program will attempt to start the service
         *  
         *  The function will also test the connection to the local Oracle service and set a 
         *  timer to run every 60 secconds
         *  
         */
        protected override void OnStart(string[] args) {
            //--------------------------------------------------------------------------------------------------------------
            // Update the service state to Start Pending.  
            ServiceStatus serviceStatus = new ServiceStatus();
            serviceStatus.dwCurrentState = ServiceState.SERVICE_START_PENDING;
            serviceStatus.dwWaitHint = 100000;
            SetServiceStatus(this.ServiceHandle, ref serviceStatus);

            Console.WriteLine("Service start pending...");

            //--------------------------------------------------------------------------------------------------------------
            // Check for Oracle and RMAN
            Console.WriteLine("Checking Oracle existance at local host...");
            if (ProgramUtils.CheckForOracle()) {
                Console.WriteLine("Oracle found!");
            } else {
                Console.WriteLine("Oracle not found. Please make sure Oracle is installed and start the service again!");
                StopService();
            }

            Console.WriteLine("Checking RMAN existance at local host...");
            if (ProgramUtils.CheckForRman()) {
                Console.WriteLine("RMAN found!");
            } else {
                Console.WriteLine("RMAN not found. Please make sure RMAN is installed and start the service again!");
                StopService();
            }
            
            //--------------------------------------------------------------------------------------------------------------
            // Check for Oracle service to be running
            Console.WriteLine("Checking Oracle service state...");
            if (ServiceUtils.IsOracleRunning()) {
                Console.WriteLine("Oracle is already running!");
            } else {
                Console.WriteLine("Oracle is not running. Attempting to start Oracle...");

                if (ServiceUtils.AttemptOracleStart()) {
                    Console.WriteLine("Oracle started successfully!");
                } else {
                    Console.WriteLine("Couldn't start Oracle. Please start Oracle manually and start the service again!");
                    StopService();
                }
            }

            //--------------------------------------------------------------------------------------------------------------
            // Check connection to local server
            Console.WriteLine("Checking connection to local Oracle server...");
            if (DatabaseUtils.TestConnection()) {
                Console.WriteLine("Connection test succeded");
            } else {
                Console.WriteLine("ERROR: Couldn't connect to local Oracle server");
                StopService();
            }

            //--------------------------------------------------------------------------------------------------------------
            // Set up a timer to trigger every 60 seconds.  
            System.Timers.Timer executorTimer = new System.Timers.Timer();
            executorTimer.Interval = 60000; // 60 seconds  
            executorTimer.Elapsed += new System.Timers.ElapsedEventHandler(this.OnExecutorTimer);
            executorTimer.Start();

            //--------------------------------------------------------------------------------------------------------------
            // If the server is on mode: MASTER, set up a timer to monitor activity every 5 minutes, 
            if(mode == ServiceModes.MASTER) {
                System.Timers.Timer monitoringTimer = new System.Timers.Timer();
                monitoringTimer.Interval = 300000; // 5 minutes 
                monitoringTimer.Elapsed += new System.Timers.ElapsedEventHandler(this.OnMonitoringTimer);
                monitoringTimer.Start();
            }

            //--------------------------------------------------------------------------------------------------------------
            // Update the service state to Running.  
            serviceStatus.dwCurrentState = ServiceState.SERVICE_RUNNING;
            SetServiceStatus(this.ServiceHandle, ref serviceStatus);

            Console.WriteLine("Service started. State: Running\n");

        }

        /**
         *  OnExecutorTimer function
         *  Runs every 60 secconds
         *  
         *  Every time it executes, it will:
         *      - Check for backup strategies to be run locally. If strategies are found, it will:
         *          - Check if the strategy isn't on queue already. If it isn't, it will:
         *              - Put the strategy on queue to run
         *              - Start a new thread to run the strategy
         *              - Insert the log back to the local and central server
         *          - If the strategy is on queue, it will idle
         *      - If no strategies are found, it will idle
         *      
         */
        public void OnExecutorTimer(object sender, System.Timers.ElapsedEventArgs args) {
            //Console.WriteLine("Service on executor timer. State: Running");
            int day = (int)DateTime.Now.DayOfWeek;
            int hour = (int)DateTime.Now.Hour;
            int minute = (int)DateTime.Now.Minute;

            ArrayList scheduled = mode == ServiceModes.MASTER ?
                DatabaseUtils.GetScheduledCentralBackups(day, hour, minute)
                : DatabaseUtils.GetScheduledBackups(day, hour, minute);
            ArrayList pending = BackupQueue.FilterPending(scheduled);

            Console.WriteLine("Scheduled: {0}", string.Join(" | ", scheduled.ToArray()));
            Console.WriteLine("Pending: {0}", string.Join(" | ", pending.ToArray()));

            foreach(string name in pending) {
                BackupQueue.AddToQueue(name);
                Strategy strategy = new Strategy(name);
                strategy.Instructions = DatabaseUtils.GetStrategyInstructions(name);
                if(strategy.Instructions.Count == 0) {
                    continue;
                }

                new Thread(() => {
                    Thread.CurrentThread.IsBackground = true;
                    Console.WriteLine("Starting thread for strategy {0}", name);

                    strategy.Log = Rman.AttemptLocalBackup(strategy);
                    DatabaseUtils.InsertLog(strategy);
                    if(mode == ServiceModes.SLAVE) {
                        DatabaseUtils.InsertLog(strategy, centralIp, centralPort);
                    }

                    if (strategy.Log.Contains("ERROR MESSAGE STACK FOLLOWS")) {
                        strategy.Error = "ERROR: An error has occured during the current backup. Please refer to the log for further explanation on why this happened";
                        DatabaseUtils.InsertError(strategy);
                        if (mode == ServiceModes.SLAVE) {
                            DatabaseUtils.InsertError(strategy, centralIp, centralPort);
                        }
                    }
                    BackupQueue.RemoveFromQueue(name);

                    Console.WriteLine("Thread finished for strategy {0}", name);

                }).Start();
            }
        }

        /**
         *  OnMonitoringTimer function
         *  Runs every 5 minutes only if the service is configured in master mode
         *  
         *  Every time it executes, it will:
         *      - Check for strategies that should've been run on the past 5 minutes.
         *        This information will be found in the log table.
         *        If the log for any given strategy isn't found:
         *          - Insert an error stating that a strategy that should have run hasn't run yet   
         *          
         */
        public void OnMonitoringTimer(object sender, System.Timers.ElapsedEventArgs args) {
            int day = (int)DateTime.Now.DayOfWeek;
            int hour = (int)DateTime.Now.Hour;
            int minute = (int)DateTime.Now.Minute;

            ArrayList scheduled = DatabaseUtils.GetBackupsInTwelfth(day, hour, minute);
            ArrayList logged = DatabaseUtils.GetLoggedBackupsInTwelfth(day, hour, minute);
            ArrayList pending = new ArrayList(scheduled);

            foreach (string strategy in logged) {
                pending.Remove(strategy);
            }

            Console.WriteLine("Scheduled in this twelfth: {0}", string.Join(" | ", scheduled.ToArray()));
            Console.WriteLine("Pending to be logged: {0}", string.Join(" | ", pending.ToArray()));

            foreach (string name in pending) {
                Strategy strategy = new Strategy(name);
                strategy.Error = "ERROR: The service has detected that an scheduled backup hasn't yet occured. Please verify that all the systems are operational";
                DatabaseUtils.InsertError(strategy);
            }
        }

        protected override void OnStop() {
            // Update the service state to Stop Pending.  
            ServiceStatus serviceStatus = new ServiceStatus();
            serviceStatus.dwCurrentState = ServiceState.SERVICE_STOP_PENDING;
            serviceStatus.dwWaitHint = 100000;
            SetServiceStatus(this.ServiceHandle, ref serviceStatus);

            // Update the service state to Stopped.  
            serviceStatus.dwCurrentState = ServiceState.SERVICE_STOPPED;
            SetServiceStatus(this.ServiceHandle, ref serviceStatus);

            Console.WriteLine("Service stopped. State: Stopped");
        }

        protected override void OnContinue() {
            Console.WriteLine("Service was paused and is now running. State: Running");
        }

        internal void TestStartupAndStop(string[] args) {
            this.OnStart(args);
            Console.ReadLine();
            this.OnStop();
        }

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool SetServiceStatus(IntPtr handle, ref ServiceStatus serviceStatus);

        private void StopService() {
            ServiceController sc = new System.ServiceProcess.ServiceController("BackupExecutorService");
            sc.WaitForStatus(System.ServiceProcess.ServiceControllerStatus.Running);
            sc.Stop();
            sc.WaitForStatus(System.ServiceProcess.ServiceControllerStatus.Stopped);
        }
    }

}


public enum ServiceState {
    SERVICE_STOPPED = 0x00000001,
    SERVICE_START_PENDING = 0x00000002,
    SERVICE_STOP_PENDING = 0x00000003,
    SERVICE_RUNNING = 0x00000004,
    SERVICE_CONTINUE_PENDING = 0x00000005,
    SERVICE_PAUSE_PENDING = 0x00000006,
    SERVICE_PAUSED = 0x00000007,
}

public enum ServiceModes {
    MASTER = 0x00000001,
    SLAVE = 0x00000002
}

[StructLayout(LayoutKind.Sequential)]
public struct ServiceStatus {
    public long dwServiceType;
    public ServiceState dwCurrentState;
    public long dwControlsAccepted;
    public long dwWin32ExitCode;
    public long dwServiceSpecificExitCode;
    public long dwCheckPoint;
    public long dwWaitHint;
};

