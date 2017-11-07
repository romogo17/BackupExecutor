using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackupExecutor {
    class Strategy {
        public string Name { get; set; }
        public string DatabaseLink { get; set; }
        public ArrayList Instructions { get; set; }
        public string Log { get; set; }
        public string Error { get; set; }


        public Strategy(string name) {
            Name = name;
            DatabaseLink = "localhost";
            Instructions = new ArrayList();
            Log = "";
            Error = "";
        }

        public override string ToString() {
            return "Name: " + Name + "\tDatabase Link: " + DatabaseLink + "\nInstructions:\n" 
                + string.Join("\n", Instructions.ToArray());
        }
    }
}
