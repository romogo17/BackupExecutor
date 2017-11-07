using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace BackupExecutor
{
    class ServiceUtils {
        private static ServiceController oracleService = new ServiceController("OracleServiceXE");

        public static bool AttemptOracleStart() {
            try {
                oracleService.Start();
                oracleService.WaitForStatus(System.ServiceProcess.ServiceControllerStatus.Running);
                return true;
            } catch (Exception ex) {
                return false;
            }
        }

        public static bool IsOracleRunning() {
            return oracleService.Status.Equals(ServiceControllerStatus.Running);
        }

    }
}
