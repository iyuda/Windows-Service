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
using CemsMobilePcrSrv;
using System.Threading;
using System.Net.Http;
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace CemsMobilePcrSrv
{
    class WindowsService : ServiceBase
    {
        static string base_url;
        static string post_from_couch_url;
        static bool TimerActive = false;
        public async  static Task<string> GetContentfromResponse(HttpResponseMessage response_msg)
        {
            Stream result=response_msg.Content.ReadAsStreamAsync().Result;
          //  Stream theStream = await response_msg.Content.ReadAsStreamAsync();
            Task<string> getStringTask = response_msg.Content.ReadAsStringAsync();
            string urlContents = await getStringTask;
            return urlContents;
            //string responseBody = await response_msg.Content.ReadAsStringAsync();
            //byte[] buffer = new byte[result.Length];
            //result.Read(buffer, 0, (int)result.Length);
            //return buffer.ToString();
        }
        public static HttpResponseMessage UrlRequest(string url, out string content, string method = "POST", string content_type = "application/json", string data = null)
        {
            try {
                Logger.LogActivity("UrlRequest for: " + url + " method: " +method);
                HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(url);
                if (url.Contains("@") && url.Contains(":"))
                {
                    int pos1 = url.IndexOf("://");
                    if (pos1 == -1) pos1 = -3;
                    int pos2 = url.IndexOf("@");
                    string credentials = url.Substring(pos1 + 3, pos2 - pos1 - 3);
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
                WebResponse response = webRequest.GetResponse();
                timer.Stop();
                
                Logger.LogActivity(method + " elapsed time: " + timer.Elapsed, "CouchStatus");
                //webRequest.CookieContainer = cookieContainer;
                StreamReader reader = new StreamReader(response.GetResponseStream());
                string responseFromServer= reader.ReadToEnd();
                HttpResponseMessage response_msg = new HttpResponseMessage(HttpStatusCode.OK);
                response_msg.Content = new StringContent(responseFromServer, Encoding.UTF8, "application/json");
                content = responseFromServer;
                //var content = ((System.Net.Http.StringContent)response_msg.Content).Value; // await response_msg.Content.ReadAsStringAsync(); 

                return response_msg;
            }
            catch (WebException ex) { 
                Logger.LogException(ex, AdditionalMsg: url + " " + method);
                HttpResponseMessage response_msg = new HttpResponseMessage(HttpStatusCode.InternalServerError);
                response_msg.Content = new StringContent(ex.Message, Encoding.UTF8, "application/json");
                content=ex.Message;
                return response_msg;
            }
            catch (Exception ex) { Logger.LogException(ex); throw ex; }
        }



        public static string GetPcrReport(string pcr_id, string agency_id, string FaxNumber, string Agency)
        {
            try
            {
                string url = WindowsService.config.GetProfileEntry("Settings", "Web API URL", "http://localhost:85/api/Couch/");
                url += String.Format("GetPcrReport?pcr_id={0}&agency_id={1}&FaxNumber={2}&agency_name={3}", pcr_id, agency_id, FaxNumber, Agency);
                string response = "";
                HttpResponseMessage status= UrlRequest(url, out response, "GET");
               
                //string OnlinePath = @"http://localhost/cemslocal/yiicems/index.php?r=Central/Report&pcrid=" + pcr_id + "&autoload=true";
                //string ReportUrl = string.Format(OnlinePath + "&btnfax=1");


                //string FileName = "PcrReport_" + pcr_id + ".pdf";

                //Fax_Email Fax_Email = new Fax_Email();
                //string DestFile = Fax_Email.CreateFile(pcr_id, ReportUrl);
                //string status = "";
                //if (!String.IsNullOrEmpty(DestFile))
                //    status = Fax_Email.sendEmailFax(DestFile, FaxNumber, Agency);
                //else
                //    status = " Error Creating PDF File";
                if (status.StatusCode == HttpStatusCode.OK)
                    if (!response.ToLower().Contains("error"))
                        return "OK";
                    else
                        return url + ":  " + response;     
                else
                    return url + ":  " + JsonConvert.SerializeObject(JsonConvert.DeserializeObject(response)); 
            }
            
            catch (Exception ex) { Logger.LogException(ex); return ex.Message; }

        }
        public static void ValidatePcrId(string CouchUrl, string php_content, ref string couch_content, ref string pcr_id)
        {
            bool was_empty = false;
            string response_string;
            if (String.IsNullOrEmpty(pcr_id))
            {
               
                was_empty = true;
                Logger.LogActivity("pcr_id is empty");
                pcr_id = (JsonMaker.GetIOSJsonExtract("$.pcr_id", php_content) ?? "").ToString();

                Logger.LogActivity("pcr_id is now " + pcr_id);

            }
            if (!String.IsNullOrEmpty(pcr_id) && was_empty)
            {
                string id = (JsonMaker.GetIOSJsonExtract("$._id", couch_content) ?? "").ToString();
                
                UrlRequest(CouchUrl + "_pcr_id", out response_string, "PUT", data: "{"+ "\"pcr_id\":\"" + pcr_id +"\"}");

                //Logger.LogActivity("pcr_id " + pcr_id + " is about to be added to json") ;
                //JsonMaker.UpdateJsonValue("$.pcr_id", pcr_id, ref couch_content);
                //Logger.LogActivity("pcr_id " + pcr_id + " has been added to json");

                //JsonMaker.UpdateJsonValue("$.last_synced", DateTime.Now.ToString(), ref couch_content);
                //string response_string;
                //UrlRequest(CouchUrl, out response_string, "PUT", data: couch_content);
                //Logger.LogActivity(CouchUrl + " PUT Response: " + response_string);
            }
            Logger.LogActivity("pcr_id " + pcr_id + " is about to be added to json");
            JsonMaker.UpdateJsonValue("$.pcr_id", pcr_id, ref couch_content);
            Logger.LogActivity("pcr_id " + pcr_id + " has been added to json");
        }
        public static bool SyncPcr(string CouchUrl, string PostFromCouchUrl, ref string couch_content, out string php_content, ref string pcr_id )
        {
            //Logger.LogActivity("ImportPcr Breakpoint 1", "Breakpoints");
            string response_string;

            //Logger.LogActivity("ImportPcr Breakpoint 2", "Breakpoints");
            if (String.IsNullOrEmpty(pcr_id))
            {
                string pcr_id_content;
                HttpResponseMessage response = UrlRequest( CouchUrl + "_pcr_id",  out pcr_id_content, "GET");
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    pcr_id = (JsonMaker.GetIOSJsonExtract("$.pcr_id", pcr_id_content)  ?? "").ToString();
                    JsonMaker.UpdateJsonValue("$.pcr_id", pcr_id, ref couch_content);
                }
            }
            UrlRequest(PostFromCouchUrl, out php_content, "POST", data: couch_content);
            //Logger.LogActivity("ImportPcr Breakpoint 3", "Breakpoints");
            object success = JsonMaker.GetIOSJsonExtract("$.success", php_content) ?? "";
            //Logger.LogActivity("ImportPcr Breakpoint 4", "Breakpoints");
            bool bool_value = false;
            Boolean.TryParse(success.ToString(), out bool_value);
            if (!bool_value) {
                HttpResponseMessage response = UrlRequest(CouchUrl, out couch_content, "GET");
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    JsonMaker.UpdateJsonValue("$.pcr_status", "sync_error", ref couch_content);
                    JsonMaker.UpdateJsonValue("$.sync_error", php_content, ref couch_content);
                    UrlRequest(CouchUrl, out response_string, "PUT", data: couch_content);
                }
                bool email_errors = bool.Parse(config.GetProfileEntry("Settings", "EMailErrors", "false"));
                if (email_errors)
                {
                    string  email_addresses = config.GetProfileEntry("Settings", "EmailAddresses", "alexander@creativeems.com");
                    Fax_Email.sendErrorEmail(email_addresses, "Sync Error:" + System.Environment.NewLine+ php_content);
                }

            }
            else
                    Logger.LogActivity(PostFromCouchUrl + System.Environment.NewLine +  couch_content + System.Environment.NewLine, "Synced_Pcrs");
            return bool_value;
        }
        public static void GetLatestPcr(string CouchUrl, ref string couch_content, string php_content, ref string pcr_id) 
        {
            string work_content;
            HttpResponseMessage response = UrlRequest(CouchUrl, out work_content, "GET");
            if (response.StatusCode == HttpStatusCode.OK && !JToken.DeepEquals(JToken.Parse(couch_content), JToken.Parse(work_content)))
            {
                couch_content = work_content;
            }
            ValidatePcrId(CouchUrl, php_content, ref couch_content, ref pcr_id);
        }


        public static Boolean UpdateEmsRun(string pcr_id)
        {
            try
            {
                string strConnect = config.GetProfileEntry("Settings", "Connection String", "Server=192.168.1.152;Database=newcems;port=3308;Uid=root;Pwd=#Creative$;");
                using (MySqlConnection cn = new MySqlConnection(strConnect))
                {
                    cn.Open();
                    string UpdateString = "update ems_run inner join pcr on pcr.ems_run=ems_run.id set status = '3' where pcr.id = '" + pcr_id + "'" + System.Environment.NewLine;
                    MySqlCommand cmd = new MySqlCommand(UpdateString, cn);
                    int rows = cmd.ExecuteNonQuery();
                    return rows > 0;
                }

            }
            catch (Exception ex) { Logger.LogException(ex); return false; }
        }
        public static string ImportPcr(string id, string rev)
        {
            try
            {
                string CouchUrl = config.GetProfileEntry("Settings", "CouchUrl", "http://192.168.1.60:5984/app-pcr/") + id;
                string PostFromCouchUrl = config.GetProfileEntry("Settings", "PostFromCouchUrl", "http://creativeemstest.com/cemslocal/yiicems/index.php?r=api/CouchDB/PostPcrFromCouch");
                string couch_content;
                string response_string;
                string php_content = "";
                //if (id != "e872d93e-6ed7-4ea2-ae3e-be8e413c85ef")
                //    return "";

                //if (id != "71a279f4-7481-486f-9178-d53d73113132") return "";
                //if (id != "9528fdd4-cf10-4dd7-9375-bc5479096496") return "";
                //if (id != "63c4b4a5-b656-440f-ba1f-2ad5c73459ff") return "";
                //if (id != "ff1081e9-444c-486c-9057-b31acf78baee") return "";
                //if (id != "2f2262c6-20cc-4b30-ac97-703de3d98d04") return "";
                //if (id != "a084f97c-7a30-4b88-8702-e2e4a72fcd5b") return "";
                //if (id != "e96de564-006a-11e8-8fac-56ce7f333890") return "";
                //if (id != "3a33eefd-0083-11e8-8fac-56ce7f333890") return "";
                //if (id != "7c6c4ab6-244d-49f4-a6ee-546e41d0b4c1") return "";
                //if (id != "18727066-5d53-42c7-a43a-36a2b8c18046") return "";
                //if (id != "b7cf4694-bfb3-4959-bb7a-727828e3ca20") return "";

                HttpResponseMessage response = UrlRequest(CouchUrl, out couch_content, "GET");
                string orig_couch_content = couch_content;
                //Task<string> content = GetContentfromResponse(response);
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string pcr_status = (JsonMaker.GetIOSJsonExtract("$.pcr_status", couch_content) ?? "").ToString();
                   // if (pcr_status != "attempted") return "";
                   //if (pcr_status!="deleted")return "";
                    Logger.LogActivity(id + " pcr status: " + pcr_status);
                    string pcr_id = (JsonMaker.GetIOSJsonExtract("$.pcr_id", couch_content) ?? "").ToString();

                    //Logger.LogActivity("ImportPcr Breakpoint 5", "Breakpoints");
                    //Logger.LogActivity("ImportPcr Breakpoint 6", "Breakpoints");

                    switch (pcr_status)
                    {
                        case "deleted":
                            rev = JsonMaker.GetIOSJsonExtract("$._rev", (object) couch_content);
                            ValidatePcrId(CouchUrl, php_content, ref couch_content, ref pcr_id);
                            UrlRequest(CouchUrl + "?rev=" + rev, out response_string, "DELETE");
                            Logger.LogActivity(CouchUrl + "?rev=" + rev + " DELETE Response: " + response_string);
                            DeletePcrID_Doc(id);
                            break;
                        //case "opened":
                        //case "attempted":
                        //    if (!SyncPcr(CouchUrl, PostFromCouchUrl, couch_content, out php_content))
                        //    {
                        //        return PostFromCouchUrl + ":  " + php_content;
                        //    }
                        //    break;
                        case "faxed":
                            JsonMaker.UpdateJsonValue("$.pcr_status", "to_be_deleted", ref couch_content);
                            UrlRequest(CouchUrl, out response_string, "PUT", data: couch_content);
                            Logger.LogActivity(CouchUrl + "?rev=" + rev + " PUT Response: " + response_string);
                            if (!SyncPcr(CouchUrl, PostFromCouchUrl, ref couch_content, out php_content, ref pcr_id))
                            {
                                Logger.LogActivity(PostFromCouchUrl + System.Environment.NewLine + php_content + System.Environment.NewLine + couch_content, "SyncErrorPcrs");
                                return PostFromCouchUrl + ":  " + php_content;
                            }
                            break;
                        case "inactive":
                        case "sync_pending":
                            if (!SyncPcr(CouchUrl, PostFromCouchUrl, ref couch_content, out php_content, ref pcr_id))
                            {
                                Logger.LogActivity(PostFromCouchUrl + System.Environment.NewLine + php_content + System.Environment.NewLine + couch_content, "SyncErrorPcrs");
                                return PostFromCouchUrl + ":  " + php_content;
                            }
                            GetLatestPcr(CouchUrl, ref couch_content, php_content, ref pcr_id);


                            //Logger.LogActivity("ImportPcr Breakpoint 7", "Breakpoints");
                            JsonMaker.UpdateJsonValue("$.pcr_status", "to_be_deleted", ref couch_content);
                            UrlRequest(CouchUrl, out response_string, "PUT", data: couch_content);
                            //UrlRequest(CouchUrl + "?rev=" + rev, out response_string, "DELETE");
                            Logger.LogActivity(CouchUrl + "?rev=" + rev + " PUT Response: " + response_string);
                            break;
                        default:
                            //Logger.LogActivity("ImportPcr Breakpoint 8", "Breakpoints");
                            if (pcr_status == "fax_pending")
                            {
                                if (!SyncPcr(CouchUrl, PostFromCouchUrl, ref couch_content, out php_content, ref pcr_id))
                                {
                                    Logger.LogActivity(PostFromCouchUrl + System.Environment.NewLine + php_content + System.Environment.NewLine + couch_content, "SyncErrorPcrs");
                                    return PostFromCouchUrl + ":  " + php_content;
                                }
                                GetLatestPcr(CouchUrl, ref couch_content, php_content, ref pcr_id);
                                //if (String.IsNullOrEmpty(pcr_id))
                                //{
                                //    pcr_id = (JsonMaker.GetIOSJsonExtract("$.pcr_id", php_content) ?? "").ToString();
                                //}
                                if (!String.IsNullOrEmpty(pcr_id))
                                {
                                    string fax_number = (JsonMaker.GetIOSJsonExtract("$.fax.number", couch_content) ?? "").ToString();
                                    string agency_name = (JsonMaker.GetIOSJsonExtract("$.fax.name", couch_content) ?? "").ToString();
                                    string agency_id = (JsonMaker.GetIOSJsonExtract("$.agency_id", couch_content) ?? "").ToString();
                                    string status = "";
                                    if (!String.IsNullOrEmpty(fax_number))
                                        status = GetPcrReport(pcr_id, agency_id, fax_number, agency_name);
                                    else
                                        status = "Fax number is empty";
                                    JsonMaker.UpdateJsonValue("$.pcr_status", status == "OK" ? "to_be_deleted" : "fax_error", ref couch_content);
                                    if (status != "OK")
                                    {
                                        JsonMaker.UpdateJsonValue("$.fax.error_message", status, ref couch_content);
                                        bool email_errors = bool.Parse(config.GetProfileEntry("Settings", "EMailErrors", "false"));
                                        if (email_errors)
                                        {
                                            string email_addresses = config.GetProfileEntry("Settings", "EmailAddresses", "alexander@creativeems.com");
                                            Fax_Email.sendErrorEmail(email_addresses, "Fax Sending Error:" + System.Environment.NewLine + status);
                                        }
                                    }
                                    UrlRequest(CouchUrl, out response_string, "PUT", data: couch_content);
                                }
                            }
                            else if (! pcr_status.EndsWith("_error") && ! pcr_status.ToLower().EndsWith("deleted"))
                            {
                                if (!SyncPcr(CouchUrl, PostFromCouchUrl, ref couch_content, out php_content, ref pcr_id))
                                {
                                    Logger.LogActivity(PostFromCouchUrl + System.Environment.NewLine + php_content + System.Environment.NewLine + couch_content, "SyncErrorPcrs");
                                    return PostFromCouchUrl + ":  " + php_content;
                                }
                                GetLatestPcr(CouchUrl, ref couch_content, php_content, ref pcr_id);
                                //if (pcr_status != "attempted")
                                  //  JsonMaker.UpdateJsonValue("$.pcr_status", "to_be_deleted", ref couch_content);
                                if (! JToken.DeepEquals(JToken.Parse(couch_content), JToken.Parse(orig_couch_content))) { 
                                    UrlRequest(CouchUrl, out response_string, "PUT", data: couch_content);
                                    //UrlRequest(CouchUrl + "?rev=" + rev, out response_string, "DELETE");
                                    Logger.LogActivity(CouchUrl + "?rev=" + rev + " PUT Response: " + response_string);
                                }
                            }
                            //ValidatePcrId(pcr_id, CouchUrl, php_content, couch_content, ref pcr_id);
                            break;
                    }
                   //ValidatePcrId(pcr_id, CouchUrl, php_content, couch_content, ref pcr_id);
                    Logger.LogActivity("ImportPcr Breakpoint 13", "Breakpoints");
                    return PostFromCouchUrl + ":  " + php_content;
                }
                else
                {
                    Logger.LogActivity("ImportPcr Breakpoint 14", "Breakpoints");
                    return CouchUrl + ":  " + JsonConvert.SerializeObject(JsonConvert.DeserializeObject(couch_content));
                }
            }
            catch (WebException ex) { Logger.LogException(ex); return ex.Message; }
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
                string content;
                HttpResponseMessage response = UrlRequest(url, out content, "GET");
                //Task<string> content = GetContentfromResponse(response);
                string Json1 = content;


                //url = base_url + "?since=" + NewSeqNo;
                //string Json2 = UrlRequest(url, "POST", "application/json");

                var SeqArray = (IEnumerable<JToken>)JsonMaker.GetIOSJsonExtract("$.results", Json1);
                if (SeqArray != null)
                {
                    foreach (JToken SeqToken in SeqArray)
                    {
                        
                        int CurrentSeqNo;
                        string strSeq = JsonMaker.GetIOSJsonExtract("$.seq", SeqToken).ToString();
                        int.TryParse(strSeq.Split('-')[0], out CurrentSeqNo);
                        Logger.LogActivity("CheckRevisions Breakpoint " + CurrentSeqNo.ToString(), "Breakpoints");
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
                        //OutString = UrlRequest(base_url + "/" + id, "GET") + System.Environment.NewLine;
                        //Logger.LogActivity(OutString);
                        string deleted = (JsonMaker.GetIOSJsonExtract("$.deleted", SeqToken) ??"").ToString();
                      
                        if (deleted.ToLower() == "true")
                        {
                            OutString = "Skipped due to deletion: " + OutString;
                            Logger.LogActivity(OutString);
                        }
                        else if (!id.StartsWith("_design") && !id.Contains("pcr_id"))
                        {
                            Logger.LogActivity(OutString);
                            Logger.LogActivity(id, "Since_" + LastSeq.Split('-')[0]);
                            string status = ImportPcr(id, rev);
                            //if (status.ToLower().Contains("error")) 
                            Logger.LogActivity("CheckRevisions Breakpoint 1", "Breakpoints");
                            Logger.LogActivity("ImportPcr Response: " + status);
                        }
                        config.UpdateAppSettings("Settings", "Last Sequence", strSeq);
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
            catch (Exception ex) { Logger.LogException(ex);  return LastSeq; }
        }
        static string LastSeq = "";
        void TimerHandler(object o, System.Timers.ElapsedEventArgs e)
        {
            //Logger.LogActivity("TimerHandler1","Test");
            //if (TimerActive) return;
            //Logger.LogActivity("TimerHandler2", "Test");
            //((System.Timers.Timer)o).Enabled = false;
            //TimerActive = true;
            //System.Windows.Forms.Application.DoEvents();
            //System.Timers.Timer t = (System.Timers.Timer)o;
            Logger.LogActivity("TimerHandler enter", "Breakpoints");
            timer.Enabled = false;
            //while (false)
            //{
            //    System.Windows.Forms.Application.DoEvents();
            //}

            string CheckUntouchedPcrsFlag = config.GetProfileEntry("Settings", "Check Untouched Pcrs", "true");

            Logger.LogActivity("CheckModifiedAgencyConfigs Breakpoint 0", "Breakpoints");
            //CheckModifiedAgencyConfigs();
            if (CheckUntouchedPcrsFlag=="true")
                CheckUntouchedPcrs();
            string Seq = CheckRevisions(LastSeq);

            //Logger.LogActivity("TimerHandler3", "Test");
            config.UpdateAppSettings("Settings", "Last Sequence", Seq.ToString());
            LastSeq = Seq;
            timer.Enabled = true;
            Logger.LogActivity("TimerHandler exit", "Breakpoints");
            //TimerActive = false;
           // ((System.Timers.Timer)o).Enabled = true;
        }
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
                
        public static ConfigCls config = new ConfigCls("SettingsPcr.config");
        public WindowsService(int dummy)
        {
        }
        public WindowsService()
        {
            this.ServiceName = "Cems Mobile Pcr Service";
            this.EventLog.Source = "Cems Mobile Pcr Service";
            this.EventLog.Log = "Application";
            
            // These Flags set whether or not to handle that specific
            //  type of event. Set to true if you need it, false otherwise.
            this.CanHandlePowerEvent = true;
            this.CanHandleSessionChangeEvent = true;
            this.CanPauseAndContinue = true;
            this.CanShutdown = true;
            this.CanStop = true;

            if (!EventLog.SourceExists("Cems Mobile Pcr Service"))
                EventLog.CreateEventSource("Cems Mobile Pcr Service", "Application");
        }
        static void Init()
        {
            base_url = config.GetProfileEntry("Settings", "CouchUrl", "http://192.168.1.60:5984/app-pcr");
            LastSeq = config.GetProfileEntry("Settings", "Last Sequence", "");
            //string customerId = WindowsService.config.GetProfileEntry("Settings", "RCA Customer ID", "RCAMBULANCE");
            //string password = WindowsService.config.GetProfileEntry("Settings", "RCA Password", "QTQnze3w");
            //string vendorKey = WindowsService.config.GetProfileEntry("Settings", "RCA Vendor Key", "CREATIVE_93S");

            Logger.LogActivity("Service Started");

            int LastSeqNo;
            int.TryParse(LastSeq.Split('-')[0], out LastSeqNo);
            Logger.LogActivity("Last Sequence: " + LastSeqNo);

            //int intFrequency;
            //int.TryParse(CheckAllDocsFrequency, out intFrequency);
            //TimerHandler(null, null);
            //System.Timers.Timer timer = new System.Timers.Timer(60000);
            //timer.Elapsed += new System.Timers.ElapsedEventHandler(TimerHandler);
            ////timer.Elapsed += async (sender, e) => await HandleTimer();
            //timer.Start();

            //System.Timers.Timer CheckAllDocs_Timer = new System.Timers.Timer(intFrequency * 60000);
            //CheckAllDocs_Timer.Elapsed += new System.Timers.ElapsedEventHandler(CheckAllDocsTimerHandler);
            ////timer.Elapsed += async (sender, e) => await HandleTimer();
            //CheckAllDocs_Timer.Start();

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
                    WindowsService WindowsService = new WindowsService(0);
                    WindowsService.InitTimers();
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
        /// 
        void InitTimers()
        {
            //string CheckAllDocsFrequency = config.GetProfileEntry("Settings", "Check All Docs Frequency In Minutes", "10");
            string frequency = config.GetProfileEntry("Settings", "Timer Frequency In Minutes", "1");
            int intFrequency = 1;
            int.TryParse(frequency, out intFrequency);
            timer.Interval= intFrequency * 60000;
            timer.Elapsed += new System.Timers.ElapsedEventHandler(TimerHandler);
            timer.Start();

            
        //    int.TryParse(CheckAllDocsFrequency, out intFrequency);
        //    CheckAllDocs_Timer = new System.Timers.Timer(intFrequency * 60000);
        //    CheckAllDocs_Timer.Elapsed += new System.Timers.ElapsedEventHandler(CheckAllDocsTimerHandler);
        //    CheckAllDocs_Timer.Start();
        }
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
        System.Timers.Timer timer= new System.Timers.Timer(60000);
        System.Timers.Timer CheckAllDocs_Timer;
        private void Worker()
        {
            Init();
            InitTimers();
//            System.Timers.Timer timer = new System.Timers.Timer(60000);
            
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
