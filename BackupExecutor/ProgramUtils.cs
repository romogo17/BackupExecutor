﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackupExecutor {
    class ProgramUtils {
        public static bool CheckForOracle() {
            return ExistsOnPath("oracle.exe");
        }

        public static bool CheckForRman() {
            return ExistsOnPath("oracle.exe");
        }

        public static bool ExistsOnPath(string fileName) {
            return GetFullPath(fileName) != null;
        }

        public static string GetFullPath(string fileName) {
            if (File.Exists(fileName))
                return Path.GetFullPath(fileName);

            var values = Environment.GetEnvironmentVariable("PATH");
            foreach (var path in values.Split(';')) {
                var fullPath = Path.Combine(path, fileName);
                if (File.Exists(fullPath))
                    return fullPath;
            }
            return null;
        }
    }
}
