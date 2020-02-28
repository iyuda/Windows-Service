using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using MySql.Data.MySqlClient;
using System.Text;
using System.Configuration;
using System.Globalization;
using System.Reflection;
using System.Data;
using System.Net;
using System.IO;
using System.Diagnostics;
using System.Windows.Forms;
using Newtonsoft.Json.Linq;
using System.ServiceProcess;
using CemsMobileF800Srv;
using System.Threading;

namespace CemsMobileF800Srv
{
    class WindowsService : ServiceBase
    {
        static string base_url;
        static string post_from_couch_url;
        static bool TimerActive = false;
        public static string UrlRequest(string url, string method = "POST", string content_type = "application/json", string data=null)
        {
            try {
                Logger.LogActivity("UrlRequest for: " + url + " method: " + method);
                HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(url);
                if (url.Contains("@") && url.Contains(":"))
                {
                    int pos1 = url.IndexOf("://");
                    if (pos1 == -1) pos1 = -3;
                    int pos2 = url.IndexOf("@");
                    string credentials = url.Substring(pos1 + 3, pos2-pos1-3);
                    string user_name = credentials.Split(':')[0];
                    string password = credentials.Split(':')[1];
                    webRequest.UseDefaultCredentials = true;
                    webRequest.PreAuthenticate = true;
                    webRequest.Credentials = new System.Net.NetworkCredential(user_name, password);
                }
                webRequest.Method = method;
                webRequest.ContentType = content_type;
                // Stream requestStream = webRequest.GetRequestStream();
                if (data != null)
                {
                    byte[] byteArray = Encoding.UTF8.GetBytes(data);
                    using (Stream dataStream = webRequest.GetRequestStream())
                    {
                        dataStream.Write(byteArray, 0, byteArray.Length);
                    }
                }
                System.Diagnostics.Stopwatch timer = new Stopwatch();
                timer.Start();
                //string credentials = String.Format("{0}:{1}", "CemsCouchAdmin", "CemsCouchAdmin!Pass5984");
                //byte[] bytes = Encoding.ASCII.GetBytes(credentials);
                //string base64 = Convert.ToBase64String(bytes);
                //string authorization = String.Concat("basic ", base64);
                //webRequest.Headers.Add("Authorization", authorization);
                WebResponse response = webRequest.GetResponse();
                timer.Stop();
                Logger.LogActivity(method + " elapsed time: " + timer.Elapsed, "CouchStatus");
                //webRequest.CookieContainer = cookieContainer;
                StreamReader reader = new StreamReader(response.GetResponseStream());
                return reader.ReadToEnd();
            }
            catch (WebException ex) { Logger.LogException(ex); return ex.Response.ResponseUri.ToString() + ":  " + ex.Message; }
            catch (Exception ex) { Logger.LogException(ex); return ex.Message; }
        }
        public static void SetRetry(string id)
            {
                try
                {
                    string url = config.GetProfileEntry("Settings", "CouchUrl", "http://192.168.1.152:5984/app-f800/") + id;
                    string response = UrlRequest(url, "GET");
                    Logger.LogActivity(url + "SetRetry Get Response: " + response);
                    string max_sync_retry = config.GetProfileEntry("Settings", "SyncRetry", "5");
                    int max_sync_retry_int = int.Parse(max_sync_retry);
                    int current_sync_retry;
                    if (!int.TryParse((JsonMaker.GetIOSJsonExtract("$.sync_retry", response)??"0").ToString(), out current_sync_retry))
                        current_sync_retry = 0;

                    if (current_sync_retry < max_sync_retry_int)
                    {
                        current_sync_retry++;
                        JsonMaker.UpdateJsonValue("$.sync_retry", current_sync_retry.ToString(), ref response);
                        string put_response = UrlRequest(url, "PUT", data: response);
                        Logger.LogActivity(url + "SetRetry PUT Response: " + put_response);
                }
                    
                }
                catch (WebException ex) { Logger.LogException(ex); }
                catch (Exception ex) { Logger.LogException(ex); }
            }
         public static string Import800(string id)
        {

            string url = config.GetProfileEntry("Settings", "CouchUrl", "http://192.168.1.152:5984/app-f800/") + id;
            string response = UrlRequest(url, "GET");
            try
            {
               
                if (!response.ToLower().Contains("error"))
                {
                    url = config.GetProfileEntry("Settings", "PostFromCouchUrl", "http://creativeemstest.com/cemslocal/yiicems/index.php?r=api/CouchDB/UpsertForm800FromCouch");
                    response = UrlRequest(url, "POST", data: response);
                    Logger.LogActivity(url + " Get Response: " + response);
                    return response;
                }
                else
                {
                    //SetRetry(url, response);  
                    Logger.LogActivity(url + " Get Response: " + response);
                    return response;
                }
            }
            catch (WebException ex) { Logger.LogException(ex); return ex.Response.ResponseUri.ToString() + ":  " + ex.Message; }
            catch (Exception ex) { Logger.LogException(ex); return ex.Message; }
        }
        public static string ImportPcr_old(string id)
        {
            try
            {
                
                //string url = post_from_couch_url + "/_changes" + (LastSeq == "" ? "" : "?since=" + LastSeq);
                //        string Json1 = UrlRequest(post_from_couch_url,method:"POST", data:);
                string url = WindowsService.config.GetProfileEntry("Settings", "Web API URL", "http://localhost:85/api/Couch/");
                url += "ImportPcr?id=" + id;
                HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(url);
                webRequest.Method = "GET";

                System.Diagnostics.Stopwatch timer = new Stopwatch();
                timer.Start();
                WebResponse response = webRequest.GetResponse();
                timer.Stop();
                StreamReader reader = new StreamReader(response.GetResponseStream());
                string ResponseString = reader.ReadToEnd();
                Logger.LogActivity(url + ":" + System.Environment.NewLine + "GET elapsed time: " + timer.Elapsed + System.Environment.NewLine + response + ":" + ResponseString, "CouchStatus");
                return ResponseString;// ((HttpWebResponse)response).StatusDescription;

            }
            catch (WebException ex) { Logger.LogException(ex); return ex.Response.ResponseUri.ToString() + ":  " + ex.Message; }
            catch (Exception ex) { Logger.LogException(ex); return ex.Message; }
        }

        public static string CheckRevisions(string LastSeq)
        {
            try
            {
                string url = base_url + "/_changes" + (LastSeq == "" ? "" : "?since=" + LastSeq);
                string Json1 = UrlRequest(url,"GET");


                //url = base_url + "?since=" + NewSeqNo;
                //string Json2 = UrlRequest(url, "POST", "application/json");

                var SeqArray = (IEnumerable<JToken>)JsonMaker.GetIOSJsonExtract("$.results", Json1);
                if (SeqArray != null)
                {
                    foreach (JToken SeqToken in SeqArray)
                    {
                        int CurrentSeqNo;
                        int.TryParse(JsonMaker.GetIOSJsonExtract("$.seq", SeqToken).ToString().Split('-')[0], out CurrentSeqNo);
                        //if (CurrentSeqNo == null) continue;
                        //if (CurrentSeqNo > NewSeqNo || CurrentSeqNo <= LastSeqNo) continue;

                        string OutString = "seq: " + CurrentSeqNo + ";  ";
                        string id = JsonMaker.GetIOSJsonExtract("$.id", SeqToken).ToString();
                        OutString += "id: " + id +";  ";
                        var ChangesArray = (IEnumerable<JToken>)JsonMaker.GetIOSJsonExtract("$.changes", SeqToken.ToString());
                        string rev = "";
                        if (ChangesArray != null)
                        {
                            rev = JsonMaker.GetIOSJsonExtract("$.rev", ChangesArray.First()).ToString();
                            OutString += "rev: " + rev + ";  ";
                        }
                        //OutString += System.Environment.NewLine;
                        //Logger.LogActivity(OutString);
                        //OutString = UrlRequest(base_url + "/" + id, "GET") + System.Environment.NewLine;
                        //Logger.LogActivity(OutString);

                        string deleted = (JsonMaker.GetIOSJsonExtract("$.deleted", SeqToken) ?? "").ToString();

                        if (deleted.ToLower() == "true")
                        {
                            OutString = "Skipped due to deletion: " + OutString;
                            Logger.LogActivity(OutString);
                        }
                        else
                        {
                            Logger.LogActivity(OutString);
                            string status = Import800(id);
                            bool success = status.ToLower().Contains("\"success\":true");
                            if (rev != "" && success)
                            {
                                url = config.GetProfileEntry("Settings", "CouchUrl", "http://192.168.1.152:5984/app-f800/") + id + "?rev=" + rev;
                                status = UrlRequest(url, "DELETE");
                                Logger.LogActivity(url + " Delete Response: " + status);
                            }
                            else if (!success) {
                                SetRetry(id);
                                bool email_errors = bool.Parse(config.GetProfileEntry("Settings", "EMailErrors", "false"));
                                if (email_errors)
                                {
                                    string email_addresses = config.GetProfileEntry("Settings", "EmailAddresses", "alexander@creativeems.com");
                                    Fax_Email.sendErrorEmail(email_addresses, "F800 Sync Error:" + System.Environment.NewLine + status);
                                }
                            }
                        }

                    }
                }
                // NewSeqNo = JsonMaker.GetIOSJsonExtract("$.last_seq", Json2).ToString();
                string NewSeq = JsonMaker.GetIOSJsonExtract("$.last_seq", Json1).ToString();
                if (NewSeq == null)
                {
                    Logger.LogActivity("Last Sequence: " + NewSeq);
                    return NewSeq;
                }
                int NewSeqNo;
                int.TryParse(NewSeq.Split('-')[0], out NewSeqNo);
                //int.TryParse(JsonMaker.GetIOSJsonExtract("$.last_seq", Json1).ToString().Split('-')[0], out NewSeqNo);
                Logger.LogActivity("Last Sequence: " + NewSeqNo);
                return NewSeq;
                //Logger.LogAction(Json1, "CouchStatus");

            }
            catch (Exception ex) { Logger.LogException(ex); return LastSeq; }
        }
        static string LastSeq = "";
        static void TimerHandler(object o, System.Timers.ElapsedEventArgs e)
        {
            Logger.LogActivity("TimerHandler1","Test");
           // if (TimerActive) return;
            Logger.LogActivity("TimerHandler2", "Test");
            TimerActive = true;
            //System.Windows.Forms.Application.DoEvents();
            string Seq = CheckRevisions(LastSeq);
            Logger.LogActivity("TimerHandler3", "Test");
            config.UpdateAppSettings("Settings", "Last Sequence", Seq.ToString());
            LastSeq = Seq;
            TimerActive = false;
        }
<<<<<<< .mine
        static Dictionary<string, DateTime> AgenciesModified = new Dictionary<string, DateTime>();
        static void CheckModifiedAgencyConfigs()
        {
            string ConfigFileRootPath = config.GetProfileEntry("Settings", "Agency Config Path", "C:\\inetpub\\wwwroot\\CemsLocal\\YiiCems\\agencySettings\\");
            string UpdateAgencyConfigUrl = config.GetProfileEntry("Settings", "Update Agency Config Url", "http://www.creativeemstest.com:85/api/Couch/UpdateAgencyConfig");

            foreach (string folder in Directory.GetDirectories(ConfigFileRootPath))
            {
                try {
                    string AgencyPath = folder + "\\agencysettings.ini";
                    string Agency = folder.Remove(0, folder.LastIndexOf("\\") + 1);
                    if (!AgenciesModified.ContainsKey(Agency))
                    {
                        string default_value = File.GetLastWriteTime(AgencyPath).ToString();
                        AgenciesModified[Agency] = Convert.ToDateTime(config.GetProfileEntry("Agency_Modified_Time_Settings", Agency, default_value));
                    }
                    if (AgenciesModified.ContainsKey(Agency)) {
                        if (File.GetLastWriteTime(AgencyPath) > AgenciesModified[Agency].AddMinutes(1)) {
                            Logger.LogActivity("CheckModifiedAgencyConfigs Breakpoint " + Agency , "Breakpoints");
                            //var folders = AgencyPath.Split(", StringSplitOptions.RemoveEmptyEntries);
                            string url = UpdateAgencyConfigUrl + "?agency_id=" + Agency + "&fax=1";
                            string content;
                            UrlRequest(url, out content, "GET");
                            DateTime GetLastWriteTime = File.GetLastWriteTime(AgencyPath);
                            AgenciesModified[Agency] = GetLastWriteTime;
                            config.UpdateAppSettings("Agency_Modified_Time_Settings", Agency, GetLastWriteTime.ToString());
                        }
                    }
                }
                catch(Exception ex)
                {
                    continue;
                }

               
            }
        }
        static void DeletePcrID_Doc(string doc_id)
        {
            string couch_content;
            string response_string;
            string url = config.GetProfileEntry("Settings", "CouchUrl", "http://192.168.1.60:5984/app-pcr/") + doc_id +"_pcr_id";
            HttpResponseMessage response = UrlRequest(url, out couch_content, "GET");
            if (response.StatusCode == HttpStatusCode.OK)
            {
                string rev = (JsonMaker.GetIOSJsonExtract("$._rev", couch_content) ?? "").ToString();
                UrlRequest(url + "?rev=" + rev, out response_string, "DELETE");
                Logger.LogActivity(url + "?rev=" + rev + " DELETE Response: " + response_string);
            }
        }
        static void CheckUntouchedPcrs()
        {
            string UntouchedTimeout = config.GetProfileEntry("Settings", "Untouched Timeout In Minutes", "10");
            int intUntouchedTimeout;
            int.TryParse(UntouchedTimeout, out intUntouchedTimeout);

            string OpenOrAttemptedTimeout = config.GetProfileEntry("Settings", "Open Or Attempted Timeout In Minutes", "360");
            int intOpenOrAttemptedTimeout;
            int.TryParse(OpenOrAttemptedTimeout, out intOpenOrAttemptedTimeout);

            string CheckAllDocsURL = config.GetProfileEntry("Settings", "Check All Docs URL", "http://192.168.1.60:5984/app-pcr/_all_docs");

            try
            {
                string url = CheckAllDocsURL;
                string content;
                HttpResponseMessage response = UrlRequest(url, out content, "GET");
                //Task<string> content = GetContentfromResponse(response);
                string Json1 = content;



                var SeqArray = (IEnumerable<JToken>)JsonMaker.GetIOSJsonExtract("$.rows", Json1);
                if (SeqArray != null)
                {
                    foreach (JToken SeqToken in SeqArray)
                    {
                     
                        string id = JsonMaker.GetIOSJsonExtract("$.id", SeqToken).ToString();
                        string CouchUrl = config.GetProfileEntry("Settings", "CouchUrl", "http://192.168.1.60:5984/app-pcr/") + id;
                        string couch_content;
                        string response_string;
                        response = UrlRequest(CouchUrl, out couch_content, "GET");

                        if (response.StatusCode == HttpStatusCode.OK) {
                            string pcr_status = (JsonMaker.GetIOSJsonExtract("$.pcr_status", couch_content) ?? "").ToString();
                            if (pcr_status == "deleted")
                            {
                                string rev = (JsonMaker.GetIOSJsonExtract("$._rev", couch_content) ?? "").ToString();
                                UrlRequest(CouchUrl + "?rev=" + rev, out response_string, "DELETE");
                                Logger.LogActivity(CouchUrl + "?rev=" + rev + " DELETE Response: " + response_string);
                                DeletePcrID_Doc(id);
                            }
                            else
                            {
                                string dateCreated = (JsonMaker.GetIOSJsonExtract("$.dateCreated", couch_content) ?? "").ToString();
                                if (Utilities.IsDate(dateCreated) && pcr_status != "to_be_deleted")
                                {
                                    bool condition1 = (pcr_status == "untouched" && DateTime.Parse(dateCreated).AddMinutes(intUntouchedTimeout) < DateTime.Now);
                                    bool condition2 = (DateTime.Parse(dateCreated).AddHours(25) < DateTime.Now);
                                    bool condition3 = ((pcr_status == "opened" || pcr_status == "attempted") && DateTime.Parse(dateCreated).AddMinutes(intOpenOrAttemptedTimeout) < DateTime.Now);
                                    if (condition3)
                                    {
                                        string PostFromCouchUrl = config.GetProfileEntry("Settings", "PostFromCouchUrl", "http://creativeemstest.com/cemslocal/yiicems/index.php?r=api/CouchDB/PostPcrFromCouch");
                                        string php_content;
                                        string pcr_id = (JsonMaker.GetIOSJsonExtract("$.pcr_id", couch_content) ?? "").ToString();
                                        if (!SyncPcr(CouchUrl, PostFromCouchUrl, ref couch_content, out php_content, ref pcr_id))
                                        {
                                            condition3 = false;
                                            Logger.LogActivity(PostFromCouchUrl + System.Environment.NewLine + php_content + System.Environment.NewLine + couch_content, "SyncErrorPcrs");
                                        }
                                    }
                                    if (condition1 || condition2 || condition3)
                                    {
                                        JsonMaker.UpdateJsonValue("$.pcr_status", "to_be_deleted", ref couch_content);
                                        UrlRequest(CouchUrl, out response_string, "PUT", data: couch_content);
                                        Logger.LogActivity("CheckUntouchedPcrs " + CouchUrl + " PUT Response: " + response_string);
                                    }

                                }
                            }
                        }    
                   }
                }
             }
            catch (Exception ex) { Logger.LogException(ex); }
        }
        static void CheckAllDocsTimerHandler(object o, System.Timers.ElapsedEventArgs e)
        {
            string CheckAllDocsURL = config.GetProfileEntry("Settings", "Check All Docs URL", "http://192.168.1.60:5984/app-pcr/_all_docs");

            try
            {
                string url = CheckAllDocsURL;
                string content;
                HttpResponseMessage response = UrlRequest(url, out content, "GET");
                //Task<string> content = GetContentfromResponse(response);
                string Json1 = content;



                var SeqArray = (IEnumerable<JToken>)JsonMaker.GetIOSJsonExtract("$.rows", Json1);
                if (SeqArray != null)
                {
                    foreach (JToken SeqToken in SeqArray)
                    {
                     
                        string id = JsonMaker.GetIOSJsonExtract("$.id", SeqToken).ToString();
                        string CouchUrl = config.GetProfileEntry("Settings", "CouchUrl", "http://192.168.1.60:5984/app-pcr/") + id;
                        string couch_content;
                        string response_string;
                        response = UrlRequest(CouchUrl, out couch_content, "GET");

                        if (response.StatusCode == HttpStatusCode.OK) {
                            string pcr_status = (JsonMaker.GetIOSJsonExtract("$.pcr_status", couch_content) ?? "").ToString();
                            string dateCreated = (JsonMaker.GetIOSJsonExtract("$.dateCreated", couch_content) ?? "").ToString();
                            if (pcr_status== "untouched" && DateTime.Parse(dateCreated).AddMinutes (10) >  DateTime.Now) {
                                    JsonMaker.UpdateJsonValue("$.pcr_status", "to_be_deleted", ref couch_content);
                                    UrlRequest(CouchUrl, out response_string, "PUT", data: couch_content);
                                    Logger.LogActivity(CouchUrl + " PUT Response: " + response_string);
                            }
                        }    
                   }
                }
                }
            catch (Exception ex) { Logger.LogException(ex); }
            }
                
||||||| .r175
        static Dictionary<string, DateTime> AgenciesModified = new Dictionary<string, DateTime>();
        static void CheckModifiedAgencyConfigs()
        {
            string ConfigFileRootPath = config.GetProfileEntry("Settings", "Agency Config Path", "C:\\inetpub\\wwwroot\\CemsLocal\\YiiCems\\agencySettings\\");
            string UpdateAgencyConfigUrl = config.GetProfileEntry("Settings", "Update Agency Config Url", "http://www.creativeemstest.com:85/api/Couch/UpdateAgencyConfig");

            foreach (string folder in Directory.GetDirectories(ConfigFileRootPath))
            {
                try {
                    string AgencyPath = folder + "\\agencysettings.ini";
                    string Agency = folder.Remove(0, folder.LastIndexOf("\\") + 1);
                    if (!AgenciesModified.ContainsKey(Agency))
                    {
                        string default_value = File.GetLastWriteTime(AgencyPath).ToString();
                        AgenciesModified[Agency] = Convert.ToDateTime(config.GetProfileEntry("Agency_Modified_Time_Settings", Agency, default_value));
                    }
                    if (AgenciesModified.ContainsKey(Agency)) {
                        if (File.GetLastWriteTime(AgencyPath) > AgenciesModified[Agency].AddMinutes(1)) {
                            Logger.LogActivity("CheckModifiedAgencyConfigs Breakpoint " + Agency , "Breakpoints");
                            //var folders = AgencyPath.Split(", StringSplitOptions.RemoveEmptyEntries);
                            string url = UpdateAgencyConfigUrl + "?agency_id=" + Agency + "&fax=1";
                            string content;
                            UrlRequest(url, out content, "GET");
                            DateTime GetLastWriteTime = File.GetLastWriteTime(AgencyPath);
                            AgenciesModified[Agency] = GetLastWriteTime;
                            config.UpdateAppSettings("Agency_Modified_Time_Settings", Agency, GetLastWriteTime.ToString());
                        }
                    }
                }
                catch(Exception ex)
                {
                    continue;
                }

               
            }
        }
        static void DeletePcrID_Doc(string doc_id)
        {
            string couch_content;
            string response_string;
            string url = config.GetProfileEntry("Settings", "CouchUrl", "http://192.168.1.60:5984/app-pcr/") + doc_id +"_pcr_id";
            HttpResponseMessage response = UrlRequest(url, out couch_content, "GET");
            if (response.StatusCode == HttpStatusCode.OK)
            {
                string rev = (JsonMaker.GetIOSJsonExtract("$._rev", couch_content) ?? "").ToString();
                UrlRequest(url + "?rev=" + rev, out response_string, "DELETE");
                Logger.LogActivity(url + "?rev=" + rev + " DELETE Response: " + response_string);
            }
        }
        static void CheckUntouchedPcrs()
        {
            string UntouchedTimeout = config.GetProfileEntry("Settings", "Untouched Timeout In Minutes", "10");
            int intUntouchedTimeout;
            int.TryParse(UntouchedTimeout, out intUntouchedTimeout);
            string CheckAllDocsURL = config.GetProfileEntry("Settings", "Check All Docs URL", "http://192.168.1.60:5984/app-pcr/_all_docs");

            try
            {
                string url = CheckAllDocsURL;
                string content;
                HttpResponseMessage response = UrlRequest(url, out content, "GET");
                //Task<string> content = GetContentfromResponse(response);
                string Json1 = content;



                var SeqArray = (IEnumerable<JToken>)JsonMaker.GetIOSJsonExtract("$.rows", Json1);
                if (SeqArray != null)
                {
                    foreach (JToken SeqToken in SeqArray)
                    {
                     
                        string id = JsonMaker.GetIOSJsonExtract("$.id", SeqToken).ToString();
                        string CouchUrl = config.GetProfileEntry("Settings", "CouchUrl", "http://192.168.1.60:5984/app-pcr/") + id;
                        string couch_content;
                        string response_string;
                        response = UrlRequest(CouchUrl, out couch_content, "GET");

                        if (response.StatusCode == HttpStatusCode.OK) {
                            string pcr_status = (JsonMaker.GetIOSJsonExtract("$.pcr_status", couch_content) ?? "").ToString();
                            if (pcr_status == "deleted")
                            {
                                string rev = (JsonMaker.GetIOSJsonExtract("$._rev", couch_content) ?? "").ToString();
                                UrlRequest(CouchUrl + "?rev=" + rev, out response_string, "DELETE");
                                Logger.LogActivity(CouchUrl + "?rev=" + rev + " DELETE Response: " + response_string);
                                DeletePcrID_Doc(id);
                            }
                            else
                            {
                                string dateCreated = (JsonMaker.GetIOSJsonExtract("$.dateCreated", couch_content) ?? "").ToString();
                                if (Utilities.IsDate(dateCreated) && pcr_status != "to_be_deleted")
                                {
                                    bool condition1 = (pcr_status == "untouched" && DateTime.Parse(dateCreated).AddMinutes(intUntouchedTimeout) < DateTime.Now);
                                    bool condition2 = (DateTime.Parse(dateCreated).AddHours(25) < DateTime.Now);
                                    if (condition1 || condition2)
                                    {
                                        JsonMaker.UpdateJsonValue("$.pcr_status", "to_be_deleted", ref couch_content);
                                        UrlRequest(CouchUrl, out response_string, "PUT", data: couch_content);
                                        Logger.LogActivity("CheckUntouchedPcrs " + CouchUrl + " PUT Response: " + response_string);
                                    }
                                }
                            }
                        }    
                   }
                }
             }
            catch (Exception ex) { Logger.LogException(ex); }
        }
        static void CheckAllDocsTimerHandler(object o, System.Timers.ElapsedEventArgs e)
        {
            string CheckAllDocsURL = config.GetProfileEntry("Settings", "Check All Docs URL", "http://192.168.1.60:5984/app-pcr/_all_docs");

            try
            {
                string url = CheckAllDocsURL;
                string content;
                HttpResponseMessage response = UrlRequest(url, out content, "GET");
                //Task<string> content = GetContentfromResponse(response);
                string Json1 = content;



                var SeqArray = (IEnumerable<JToken>)JsonMaker.GetIOSJsonExtract("$.rows", Json1);
                if (SeqArray != null)
                {
                    foreach (JToken SeqToken in SeqArray)
                    {
                     
                        string id = JsonMaker.GetIOSJsonExtract("$.id", SeqToken).ToString();
                        string CouchUrl = config.GetProfileEntry("Settings", "CouchUrl", "http://192.168.1.60:5984/app-pcr/") + id;
                        string couch_content;
                        string response_string;
                        response = UrlRequest(CouchUrl, out couch_content, "GET");

                        if (response.StatusCode == HttpStatusCode.OK) {
                            string pcr_status = (JsonMaker.GetIOSJsonExtract("$.pcr_status", couch_content) ?? "").ToString();
                            string dateCreated = (JsonMaker.GetIOSJsonExtract("$.dateCreated", couch_content) ?? "").ToString();
                            if (pcr_status== "untouched" && DateTime.Parse(dateCreated).AddMinutes (10) >  DateTime.Now) {
                                    JsonMaker.UpdateJsonValue("$.pcr_status", "to_be_deleted", ref couch_content);
                                    UrlRequest(CouchUrl, out response_string, "PUT", data: couch_content);
                                    Logger.LogActivity(CouchUrl + " PUT Response: " + response_string);
                            }
                        }    
                   }
                }
                }
            catch (Exception ex) { Logger.LogException(ex); }
            }
                
=======
>>>>>>> .r181
        public static ConfigCls config = new ConfigCls("SettingsPcr.config");
        public WindowsService()
        {
            this.ServiceName = "Cems Mobile F800 Service";
            this.EventLog.Source = "Cems Mobile F800 Service";
            this.EventLog.Log = "Application";
            
            // These Flags set whether or not to handle that specific
            //  type of event. Set to true if you need it, false otherwise.
            this.CanHandlePowerEvent = true;
            this.CanHandleSessionChangeEvent = true;
            this.CanPauseAndContinue = true;
            this.CanShutdown = true;
            this.CanStop = true;

            if (!EventLog.SourceExists("Cems Mobile F800 Service"))
                EventLog.CreateEventSource("Cems Mobile F800 Service", "Application");
        }
        static void Init()
        {
            base_url = config.GetProfileEntry("Settings", "CouchUrl", "http://192.168.1.152:5984/app-f800");
            
            string frequency = config.GetProfileEntry("Settings", "Timer Frequency In Minutes", "1");
            int timer_frequency = 1;
            int.TryParse(frequency, out timer_frequency);

            LastSeq = config.GetProfileEntry("Settings", "Last Sequence", "");
            Logger.LogActivity("Service Started");

            int LastSeqNo;
            int.TryParse(LastSeq.Split('-')[0], out LastSeqNo);
            Logger.LogActivity("Last Sequence: " + LastSeqNo);

            System.Timers.Timer timer = new System.Timers.Timer(timer_frequency & 60000);
            timer.Elapsed += new System.Timers.ElapsedEventHandler(TimerHandler);
            //timer.Elapsed += async (sender, e) => await HandleTimer();
            timer.Start();

            //var timer = new System.Threading.Timer(
            //e => Timer(),
            //null,
            //TimeSpan.Zero,
            //TimeSpan.FromMinutes(1));
        }
        /// <summary>
        /// The Main Thread: This is where your Service is Run.
        /// </summary>
        static void Main()
        {
            Logger.LogActivity("Main" );
            try { 
            if (System.Environment.GetCommandLineArgs().Length > 1)
                if (System.Environment.GetCommandLineArgs()[1] == "debug")
                {
                    Init();
                    while (true)
                    {
                        System.Windows.Forms.Application.DoEvents();
                    }
                }
            Logger.LogActivity("Enter main");
            ServiceBase.Run(new WindowsService());
            Logger.LogActivity("Service ran");
            }
            catch (Exception ex) { Logger.LogException(ex); }
        }

        /// <summary>
        /// Dispose of objects that need it here.
        /// </summary>
        /// <param name="disposing">Whether or not disposing is going on.</param>
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }

        /// <summary>
        /// OnStart: Put startup code here
        ///  - Start threads, get inital data, etc.
        /// </summary>
        /// <param name="args"></param>
        /// 
        bool serviceStarted = false;
        Thread WorkerThread;
        private void Worker()
        {
            Init();
        }
        protected override void OnStart(string[] args)
        {
            System.Diagnostics.Debugger.Launch();  
            this.EventLog.WriteEntry("OnStart");
            Logger.LogActivity("OnStart");
            ThreadStart start = new ThreadStart(Worker); // FaxWorker is where the work gets done
            WorkerThread = new Thread(start);

            // set flag to indicate worker thread is active
            serviceStarted = true;

            // start threads
            WorkerThread.Start();
            base.OnStart(args);
            //File.AppendAllText("c:\\CR_test.txt", "2");
            //Init();

        }

        /// <summary>
        /// OnStop: Put your stop code here
        /// - Stop threads, set final data, etc.
        /// </summary>
        protected override void OnStop()
        {
            serviceStarted = false;
            Logger.LogActivity("OnStop");
            // wait for threads to stop
            WorkerThread.Join(60);

            //try
            //{
            //    string error = "";
            //    System.Runtime.Remoting.Messaging.SMS.SendSMSTextAsync("5555555555", "Messaging Service stopped on " + System.Net.Dns.GetHostName(), ref error);
            //}
            //catch
            //{
            //    // yes eat exception if text failed
            //}
           // File.AppendAllText("c:\\CR_test.txt", "STOP");
            base.OnStop();
        }

        /// <summary>
        /// OnPause: Put your pause code here
        /// - Pause working threads, etc.
        /// </summary>
        protected override void OnPause()
        {
            base.OnPause();
        }

        /// <summary>
        /// OnContinue: Put your continue code here
        /// - Un-pause working threads, etc.
        /// </summary>
        protected override void OnContinue()
        {
            base.OnContinue();
        }

        /// <summary>
        /// OnShutdown(): Called when the System is shutting down
        /// - Put code here when you need special handling
        ///   of code that deals with a system shutdown, such
        ///   as saving special data before shutdown.
        /// </summary>
        protected override void OnShutdown()
        {
            base.OnShutdown();
        }

        /// <summary>
        /// OnCustomCommand(): If you need to send a command to your
        ///   service without the need for Remoting or Sockets, use
        ///   this method to do custom methods.
        /// </summary>
        /// <param name="command">Arbitrary Integer between 128 & 256</param>
        protected override void OnCustomCommand(int command)
        {
            //  A custom command can be sent to a service by using this method:
            //#  int command = 128; //Some Arbitrary number between 128 & 256
            //#  ServiceController sc = new ServiceController("NameOfService");
            //#  sc.ExecuteCommand(command);

            base.OnCustomCommand(command);
        }

        /// <summary>
        /// OnPowerEvent(): Useful for detecting power status changes,
        ///   such as going into Suspend mode or Low Battery for laptops.
        /// </summary>
        /// <param name="powerStatus">The Power Broadcase Status (BatteryLow, Suspend, etc.)</param>
        protected override bool OnPowerEvent(PowerBroadcastStatus powerStatus)
        {
            return base.OnPowerEvent(powerStatus);
        }

        /// <summary>
        /// OnSessionChange(): To handle a change event from a Terminal Server session.
        ///   Useful if you need to determine when a user logs in remotely or logs off,
        ///   or when someone logs into the console.
        /// </summary>
        /// <param name="changeDescription"></param>
        protected override void OnSessionChange(SessionChangeDescription changeDescription)
        {
            base.OnSessionChange(changeDescription);
        }
    }
}
