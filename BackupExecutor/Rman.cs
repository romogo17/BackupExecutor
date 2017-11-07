using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackupExecutor {
    class Rman {

        public static string AttemptLocalBackup(Strategy strategy) {
            Process process = new Process();
            process.StartInfo.FileName = "rman.exe";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardInput = true;
            process.StartInfo.RedirectStandardOutput = true;
            process.Start();

            using (StreamWriter sw = process.StandardInput) {
                if (sw.BaseStream.CanWrite) {
                    sw.WriteLine("CONNECT target system/manager@XE");
                    foreach(string instruction in strategy.Instructions) {
                        sw.WriteLine(instruction);
                    }
                    sw.WriteLine("EXIT;");
                }
            }

            // Synchronously read the standard output of the spawned process. 
            StreamReader reader = process.StandardOutput;
            string output = reader.ReadToEnd();

            process.WaitForExit();
            process.Close();

            // Write the redirected output to this application's window.
            //Console.WriteLine("The log of the process is: \n {0}", output);
            return output;
        }

    }
}
