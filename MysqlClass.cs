using System;
using System.IO;
using MySql.Data.MySqlClient;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace CheckRevisions
{


    using Microsoft.VisualBasic;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Data;
    using System.Diagnostics;

    public class MySqlClass
    {
        public string mysqlHost = "127.0.0.1";
        public string mysqlUserName = "ck_conxt_value";
        public string mysqlPassword = "2014208044";
        public string mysqlDatabase = "ck_general";
        public string mysqlTableName = "ck_reporter_web";
        private string vbCrLf = System.Environment.NewLine;
        private string ConnectionString;
        //private string LocalConnectionString;
        private MySqlConnection conn = new MySqlConnection();


        public bool LogActivity(string TypeCheck, string Duty, string Status, string LastTimeCheck, string NextTimeCheck, string Interval, bool UsePost = true)
        {
            try
            {

                //ConfigProcessing.config.UpdateAppSettings(Duty, "Last Time Check", LastTimeCheck);
                //ConfigProcessing.config.UpdateAppSettings(Duty, "Next Time Check", NextTimeCheck);

                string SqlString = "insert into " + mysqlTableName;
                SqlString += "(`type check`, duty, status, curr_timecheck, next_timecheck, interval_check)";
                SqlString += " values  ('" + TypeCheck + "','" + Duty + "','" + Status + "','" + LastTimeCheck + "','" + NextTimeCheck + "','" + Interval + "')";
                //SqlString = "insert into ck_reporter_web(`type check`, `duty`, `status`, `curr_timecheck`,";
                //SqlString += "`next_timecheck`, `interval_check`) values  ('local Status','pdfsvr','system ";
                //SqlString += " online','2014-06-28 23:45:41','2014-06-28 23:50:41','5')";
                bool PostStatus = UsePost;
                if (UsePost)
                {
                    PostStatus = WebPostClass.PostMsg(SqlString + ";");
                    //                 if (!PostStatus)
                    //                   {
                    if (CheckConnection(ConnectionString))
                    {
                        MySqlCommand cmd = new MySqlCommand(SqlString, conn);
                        cmd.ExecuteNonQuery();
                    }

                }
                else
                {
                    CheckConnection();
                    MySqlCommand cmd = new MySqlCommand(SqlString, conn);
                    cmd.ExecuteNonQuery();
                }
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogException("LogActivity" + vbCrLf + ex.Message, true);

                return false;
            }
        }

        public DataTable GetFailedPosts()
        {
            try
            {
                string SqlString = "select * from failed_posts";
                CheckConnection();
                MySqlCommand cmd = new MySqlCommand(SqlString, conn);
                DataTable Messages = new DataTable();
                Messages.Load(cmd.ExecuteReader());
                Messages.Columns.Add("Failed");
                return Messages;
            }
            catch (Exception ex)
            {
                Utilities.LogException("LogFailedPost" + vbCrLf + ex.Message, true);
                return null;
            }
        }
        public bool DeleteFailedPosts(DataTable Messages)
        {
            try
            {
                foreach (DataRow row in Messages.Rows)
                {
                    if (row["Failed"] != null)
                        if (Convert.ToBoolean(row["Failed"]) == true)
                            continue;
                    string SqlString = "delete from failed_posts where id = " + row["ID"];
                    CheckConnection();
                    MySqlCommand cmd = new MySqlCommand(SqlString, conn);
                    cmd.ExecuteNonQuery();
                }
                return true;
            }
            catch (Exception ex)
            {
                Utilities.LogException("LogFailedPost" + vbCrLf + ex.Message, true);
                return false;
            }
        }
        public bool LogFailedPost(string Message)
        {
            try
            {
                string SqlString = "insert into failed_posts (Message) values ('" + Message.Replace("'", "''") + "')";
                CheckConnection();
                MySqlCommand cmd = new MySqlCommand(SqlString, conn);
                cmd.ExecuteNonQuery();
                return true;
            }
            catch (Exception ex)
            {
                Utilities.LogException("LogFailedPost" + vbCrLf + ex.Message, true);
                return false;
            }
        }

        public DateTime GetLastPowerCheckTime()
        {
            try
            {
                string SqlString = "select max(curr_timecheck) from " + mysqlTableName + " where [type check]='power failure'";
                CheckConnection();
                MySqlCommand cmd = new MySqlCommand(SqlString, conn);
                string strMaxTime = cmd.ExecuteScalar().ToString();
                if (Information.IsDate(strMaxTime))
                    return Convert.ToDateTime(strMaxTime);
                else
                    return DateTime.Now;
            }
            catch (Exception ex)
            {
                Utilities.LogException("GetLastPowerCheckTime" + vbCrLf + ex.Message, true);

                return DateTime.Now;
            }
        }

        public void LogErrors(string LogFile)
        {
            try
            {
                string NewLogFile = LogFile + "." + String.Format("HHmmss", System.DateTime.Now);
                File.Move(LogFile, NewLogFile);
                StreamReader reader = new StreamReader(NewLogFile);
                string Line = null;
                string NextLine = null;
                string ErrorMsg = null;
                while (!reader.EndOfStream)
                {
                    Line = reader.ReadLine();
                    if (Information.IsDate(Line))
                    {
                        NextLine = reader.ReadLine();
                        ErrorMsg = "";
                        while (!string.IsNullOrEmpty(NextLine) & !reader.EndOfStream)
                        {
                            ErrorMsg += NextLine + " - ";
                            NextLine = reader.ReadLine();
                            if (string.IsNullOrEmpty(NextLine) | reader.EndOfStream)
                            {
                                ErrorMsg = ErrorMsg.Trim();
                                if (ErrorMsg.EndsWith("-"))
                                {
                                    ErrorMsg = ErrorMsg.Remove(ErrorMsg.Length - 1, 1);
                                }
                            }
                        }
                        //if (!string.IsNullOrEmpty(ErrorMsg))                            UpdateChatMeeting(ErrorMsg, Line);
                    }
                }
                reader.Close();
            }
            catch
            {
            }
        }
        public DataTable GetComputerNames(bool UsePost = false)
        {
            try
            {
                string SqlString = "select * from ck_reporter_web_computers";

                CheckConnection();
                MySqlCommand cmd = new MySqlCommand(SqlString, conn);
                MySqlDataReader Reader = cmd.ExecuteReader();
                DataTable DataTable = new DataTable();
                DataTable.Load(Reader);
                return DataTable;
            }
            catch (Exception ex)
            {
                Utilities.LogException("GetCurrentUsers" + vbCrLf + ex.Message, true);

                return null;
            }
        }
        public bool CheckConnection(string ConnectionString = "")
        {
            try
            {
                if (ConnectionString == "") ConnectionString = this.ConnectionString;
                if (conn.State != ConnectionState.Open)
                {
                    conn.ConnectionString = ConnectionString;
                    conn.Open();
                }
                if (File.Exists(Utilities.LogsDirectory + "\\Errors.log"))
                {
                    LogErrors(Utilities.LogsDirectory + "\\Errors.log");
                }
                return true;
            }
            catch (Exception ex)
            {
                Utilities.LogException("CheckConnection:" + vbCrLf + ConnectionString + vbCrLf + ex.Message, true);

                return false;

            }


        }
        public MySqlClass()
        {
            ConnectionString = "Server=" + mysqlHost + ";UID=" + mysqlUserName + ";PWD=" + mysqlPassword + ";Database=" + mysqlDatabase;
            //LocalConnectionString = "Server=paul;UID=" + mysqlUserName + ";PWD=" + mysqlPassword + ";Database=" + mysqlDatabase;
            //        LocalConnectionString = "Server=paul;Database="+Path.GetDirectoryName(Application.ExecutablePath) + "\\Reporter.mwb";
        }

    }

}