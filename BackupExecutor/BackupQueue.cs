using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackupExecutor {
    class BackupQueue {

        private static  ArrayList backupQueue = ArrayList.Synchronized(new ArrayList());
        private static readonly BackupQueue instance = new BackupQueue();

        private BackupQueue() {
            backupQueue.Add("EST_1509986184");
        }

        public static BackupQueue Instance {
            get {
                return instance;
            }
        }

        public static ArrayList FilterPending (ArrayList scheduled) {
            ArrayList worker = new ArrayList(scheduled);
            foreach (string strategy in backupQueue) {
                worker.Remove(strategy);
            }
            return worker;
        }

        public static bool AddToQueue(string strategy) {
            try {
                backupQueue.Add(strategy);
                return true;
            } catch (Exception ex) {
                System.Console.WriteLine("ERROR: Exception {0}", ex.Message);
                return false;
            }
        }

        public static bool RemoveFromQueue(string strategy) {
            try {
                backupQueue.Remove(strategy);
                return true;
            } catch (Exception ex) {
                System.Console.WriteLine("ERROR: Exception {0}", ex.Message);
                return false;
            }
        }



    }
}