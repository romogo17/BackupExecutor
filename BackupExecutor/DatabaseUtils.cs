using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using System.Data;
using System.Collections;

namespace BackupExecutor {
    class DatabaseUtils {

        public static bool TestConnection(string ip = "localhost", string port = "1521") {

            string connectionString = "User Id=SYSTEM;Password=MANAGER;Data Source=" + ip + ":" + port + "/XE";

            using (OracleConnection objConn = new OracleConnection(connectionString)) {

                try {

                    objConn.Open();
                    if ((objConn.State & ConnectionState.Open) > 0) {
                        return true;
                    } else {
                        return false;
                    }

                } catch (OracleException ex) {
                    System.Console.WriteLine("ERROR: Exception {0}", ex.Message);
                    return false;
                } finally {
                    objConn.Close();
                }

            }
        }

        public static ArrayList GetScheduledBackups(int day, int hour, int minutes, string ip = "localhost", string port = "1521") {
            string connectionString = "User Id=SYSTEM;Password=MANAGER;Data Source=" + ip + ":" + port + "/XE";

            using (OracleConnection objConn = new OracleConnection(connectionString)) {
                DataSet data = new DataSet();

                // Create and execute the command
                OracleCommand objCmd = new OracleCommand();
                objCmd.Connection = objConn;
                objCmd.CommandText = "scheduled_backups";
                objCmd.CommandType = CommandType.StoredProcedure;

                // Set parameters
                OracleParameter retParam = objCmd.Parameters.Add("return_value", OracleDbType.RefCursor, ParameterDirection.ReturnValue);
                objCmd.Parameters.Add("d", OracleDbType.Int32, day, System.Data.ParameterDirection.Input);
                objCmd.Parameters.Add("h", OracleDbType.Int32, hour, System.Data.ParameterDirection.Input);
                objCmd.Parameters.Add("m", OracleDbType.Int32, minutes, System.Data.ParameterDirection.Input);

                try {
                    objConn.Open();
                    objCmd.ExecuteNonQuery();

                    OracleDataAdapter a = new OracleDataAdapter(objCmd);
                    //a.TableMappings.Add("MyTable", "sample_table"); // possible need for this
                    a.Fill(data);

                } catch (OracleException ex) {
                    System.Console.WriteLine("ERROR: Exception {0}", ex.Message);
                    return new ArrayList();
                } finally {
                    objConn.Close();
                    objConn.Dispose();
                }

                ArrayList list = new ArrayList();
                foreach (DataRow dr in data.Tables[0].Rows) {
                    list.Add(dr.ItemArray[0]);
                }
                return list;
            }
        }

        public static ArrayList GetBackupsInTwelfth(int day, int hour, int minutes, string ip = "localhost", string port = "1521") {
            string connectionString = "User Id=SYSTEM;Password=MANAGER;Data Source=" + ip + ":" + port + "/XE";
            int IMin, FMin;

            if      (minutes >= 0  && minutes < 5)  { IMin = 0;  FMin = 4; }
            else if (minutes >= 5  && minutes < 10) { IMin = 5;  FMin = 9; }
            else if (minutes >= 10 && minutes < 15) { IMin = 10; FMin = 14; }
            else if (minutes >= 15 && minutes < 20) { IMin = 15; FMin = 19; }
            else if (minutes >= 20 && minutes < 25) { IMin = 20; FMin = 24; }
            else if (minutes >= 25 && minutes < 30) { IMin = 25; FMin = 29; }
            else if (minutes >= 30 && minutes < 35) { IMin = 30; FMin = 34; }
            else if (minutes >= 35 && minutes < 40) { IMin = 35; FMin = 39; }
            else if (minutes >= 40 && minutes < 45) { IMin = 40; FMin = 44; }
            else if (minutes >= 45 && minutes < 50) { IMin = 45; FMin = 49; }
            else if (minutes >= 50 && minutes < 55) { IMin = 50; FMin = 54; }
            else                                    { IMin = 55; FMin = 59; }

            using (OracleConnection objConn = new OracleConnection(connectionString)) {
                DataSet data = new DataSet();

                // Create and execute the command
                OracleCommand objCmd = new OracleCommand();
                objCmd.Connection = objConn;
                objCmd.CommandText = "twelfth_backups";
                objCmd.CommandType = CommandType.StoredProcedure;

                // Set parameters
                OracleParameter retParam = objCmd.Parameters.Add("return_value", OracleDbType.RefCursor, ParameterDirection.ReturnValue);
                objCmd.Parameters.Add("d", OracleDbType.Int32, day, System.Data.ParameterDirection.Input);
                objCmd.Parameters.Add("h", OracleDbType.Int32, hour, System.Data.ParameterDirection.Input);
                objCmd.Parameters.Add("m_i", OracleDbType.Int32, IMin, System.Data.ParameterDirection.Input);
                objCmd.Parameters.Add("m_f", OracleDbType.Int32, FMin, System.Data.ParameterDirection.Input);

                try {
                    objConn.Open();
                    objCmd.ExecuteNonQuery();

                    OracleDataAdapter a = new OracleDataAdapter(objCmd);
                    //a.TableMappings.Add("MyTable", "sample_table"); // possible need for this
                    a.Fill(data);

                } catch (OracleException ex) {
                    System.Console.WriteLine("ERROR: Exception {0}", ex.Message);
                    return new ArrayList();
                } finally {
                    objConn.Close();
                    objConn.Dispose();
                }

                ArrayList list = new ArrayList();
                foreach (DataRow dr in data.Tables[0].Rows) {
                    list.Add(dr.ItemArray[0]);
                }
                return list;
            }
        }

        public static ArrayList GetLoggedBackupsInTwelfth(int day, int hour, int minutes, string ip = "localhost", string port = "1521") {
            string connectionString = "User Id=SYSTEM;Password=MANAGER;Data Source=" + ip + ":" + port + "/XE";
            int IMin, FMin;
            
            if      (minutes >= 0  && minutes < 5)  { IMin = 0;  FMin = 4; }
            else if (minutes >= 5  && minutes < 10) { IMin = 5;  FMin = 9; }
            else if (minutes >= 10 && minutes < 15) { IMin = 10; FMin = 14; }
            else if (minutes >= 15 && minutes < 20) { IMin = 15; FMin = 19; }
            else if (minutes >= 20 && minutes < 25) { IMin = 20; FMin = 24; }
            else if (minutes >= 25 && minutes < 30) { IMin = 25; FMin = 29; }
            else if (minutes >= 30 && minutes < 35) { IMin = 30; FMin = 34; }
            else if (minutes >= 35 && minutes < 40) { IMin = 35; FMin = 39; }
            else if (minutes >= 40 && minutes < 45) { IMin = 40; FMin = 44; }
            else if (minutes >= 45 && minutes < 50) { IMin = 45; FMin = 49; }
            else if (minutes >= 50 && minutes < 55) { IMin = 50; FMin = 54; }
            else                                    { IMin = 55; FMin = 59; }

            using (OracleConnection objConn = new OracleConnection(connectionString)) {
                DataSet data = new DataSet();

                // Create and execute the command
                OracleCommand objCmd = new OracleCommand();
                objCmd.Connection = objConn;
                objCmd.CommandText = "logged_twelfth_backups";
                objCmd.CommandType = CommandType.StoredProcedure;

                // Set parameters
                OracleParameter retParam = objCmd.Parameters.Add("return_value", OracleDbType.RefCursor, ParameterDirection.ReturnValue);
                objCmd.Parameters.Add("d", OracleDbType.Int32, day, System.Data.ParameterDirection.Input);
                objCmd.Parameters.Add("h", OracleDbType.Int32, hour, System.Data.ParameterDirection.Input);
                objCmd.Parameters.Add("m_i", OracleDbType.Int32, IMin, System.Data.ParameterDirection.Input);
                objCmd.Parameters.Add("m_f", OracleDbType.Int32, FMin, System.Data.ParameterDirection.Input);

                try {
                    objConn.Open();
                    objCmd.ExecuteNonQuery();

                    OracleDataAdapter a = new OracleDataAdapter(objCmd);
                    //a.TableMappings.Add("MyTable", "sample_table"); // possible need for this
                    a.Fill(data);

                } catch (OracleException ex) {
                    System.Console.WriteLine("ERROR: Exception {0}", ex.Message);
                    return new ArrayList();
                } finally {
                    objConn.Close();
                    objConn.Dispose();
                }

                ArrayList list = new ArrayList();
                foreach (DataRow dr in data.Tables[0].Rows) {
                    list.Add(dr.ItemArray[0]);
                }
                return list;
            }
        }

        public static string GetStrategyLink(string name) {
            throw new NotImplementedException();
        }

        public static ArrayList GetStrategyInstructions(string name, string ip = "localhost", string port = "1521") {
            string connectionString = "User Id=SYSTEM;Password=MANAGER;Data Source=" + ip + ":" + port + "/XE";

            using (OracleConnection objConn = new OracleConnection(connectionString)) {
                DataSet data = new DataSet();

                // Create and execute the command
                OracleCommand objCmd = new OracleCommand();
                objCmd.Connection = objConn;
                objCmd.CommandText = "strategy_instructions";
                objCmd.CommandType = CommandType.StoredProcedure;

                // Set parameters
                OracleParameter retParam = objCmd.Parameters.Add("return_value", OracleDbType.RefCursor, ParameterDirection.ReturnValue);
                objCmd.Parameters.Add("name", OracleDbType.Varchar2, name, System.Data.ParameterDirection.Input);


                try {
                    objConn.Open();
                    objCmd.ExecuteNonQuery();

                    OracleDataAdapter a = new OracleDataAdapter(objCmd);
                    //a.TableMappings.Add("MyTable", "sample_table"); // possible need for this
                    a.Fill(data);

                } catch (OracleException ex) {
                    System.Console.WriteLine("ERROR: Exception {0}", ex.Message);
                    return new ArrayList();
                } finally {
                    objConn.Close();
                    objConn.Dispose();
                }

                ArrayList list = new ArrayList();
                foreach (DataRow dr in data.Tables[0].Rows) {
                    list.Add(dr.ItemArray[0]);
                }
                return list;
            }
        }

        public static bool InsertLog(Strategy strategy, string ip = "localhost", string port = "1521") {
            string connectionString = "User Id=SYSTEM;Password=MANAGER;Data Source=" + ip + ":" + port + "/XE";

            using (OracleConnection objConn = new OracleConnection(connectionString)) {
                // Create and execute the command
                OracleCommand objCmd = new OracleCommand();
                objCmd.Connection = objConn;
                objCmd.CommandText = "insert_log";
                objCmd.CommandType = CommandType.StoredProcedure;

                // Set parameters
                objCmd.Parameters.Add("name", OracleDbType.Varchar2, strategy.Name, System.Data.ParameterDirection.Input);
                objCmd.Parameters.Add("log", OracleDbType.Clob, strategy.Log, System.Data.ParameterDirection.Input);

                try {
                    objConn.Open();
                    objCmd.ExecuteNonQuery();
                } catch (OracleException ex) {
                    System.Console.WriteLine("ERROR: Exception {0}", ex.Message);
                    return false;
                } finally {
                    objConn.Close();
                    objConn.Dispose();
                }
                return true;
            }
        }

        public static bool InsertError(Strategy strategy, string ip = "localhost", string port = "1521") {
            string connectionString = "User Id=SYSTEM;Password=MANAGER;Data Source=" + ip + ":" + port + "/XE";

            using (OracleConnection objConn = new OracleConnection(connectionString)) {
                // Create and execute the command
                OracleCommand objCmd = new OracleCommand();
                objCmd.Connection = objConn;
                objCmd.CommandText = "insert_error";
                objCmd.CommandType = CommandType.StoredProcedure;

                // Set parameters
                objCmd.Parameters.Add("name", OracleDbType.Varchar2, strategy.Name, System.Data.ParameterDirection.Input);
                objCmd.Parameters.Add("msg", OracleDbType.Varchar2, strategy.Error, System.Data.ParameterDirection.Input);

                try {
                    objConn.Open();
                    objCmd.ExecuteNonQuery();
                } catch (OracleException ex) {
                    System.Console.WriteLine("ERROR: Exception {0}", ex.Message);
                    return false;
                } finally {
                    objConn.Close();
                    objConn.Dispose();
                }
                return true;
            }
        }
    }
}
