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
using WindowsService;

namespace CheckRevisions
{
    class WindowsService2 : ServiceBase
    {
        static string base_url = "http://192.168.1.60:5984/pcr-test";
        static bool TimerActive = false;
        public static string UrlRequest(string url, string method = "POST", string content_type = "application/json")
        {
            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(url);
            webRequest.Method = method;
            webRequest.ContentType = content_type;
            // Stream requestStream = webRequest.GetRequestStream();

            System.Diagnostics.Stopwatch timer = new Stopwatch();
            timer.Start();
            WebResponse response = webRequest.GetResponse();
            timer.Stop();
            Logger.LogActivity("POST elapsed time: " + timer.Elapsed, "CouchStatus");
            //webRequest.CookieContainer = cookieContainer;
            StreamReader reader = new StreamReader(response.GetResponseStream());
            return reader.ReadToEnd();
        }
        public static string CheckRevisions(string LastSeqNo)
        {
            try
            {
                string url = base_url + "/_changes" + (LastSeqNo == "" ? "" : "?since=" + LastSeqNo);
                string Json1 = UrlRequest(url);


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

                        string OutString = "seq: " + CurrentSeqNo + System.Environment.NewLine;
                        string id = JsonMaker.GetIOSJsonExtract("$.id", SeqToken).ToString();
                        OutString += "id: " + id + System.Environment.NewLine;
                        var ChangesArray = (IEnumerable<JToken>)JsonMaker.GetIOSJsonExtract("$.changes", SeqToken.ToString());
                        if (ChangesArray != null)
                        {
                            string rev = JsonMaker.GetIOSJsonExtract("$.rev", ChangesArray.First()).ToString();
                            OutString += "rev: " + rev + System.Environment.NewLine;
                        }
                        OutString += System.Environment.NewLine;
                        OutString += UrlRequest(base_url + "/" + id, "GET") + System.Environment.NewLine;
                        Logger.LogActivity(OutString);
                        string PostJson = Newtonsoft.Json.JsonConvert.SerializeObject(Json1);
                    }
                }
                // NewSeqNo = JsonMaker.GetIOSJsonExtract("$.last_seq", Json2).ToString();
                string NewSeqNo = JsonMaker.GetIOSJsonExtract("$.last_seq", Json1).ToString();
                //int.TryParse(JsonMaker.GetIOSJsonExtract("$.last_seq", Json1).ToString().Split('-')[0], out NewSeqNo);
                if (NewSeqNo == null) return LastSeqNo;
                return NewSeqNo;
                //Logger.LogAction(Json1, "CouchStatus");

            }
            catch (Exception ex) { Logger.LogException(ex); return LastSeqNo; }
        }
        static string LastSeqNo = "";
        static void Timer()
        {
            if (TimerActive) return;
            TimerActive = true;
            System.Windows.Forms.Application.DoEvents();
            string SeqNo = CheckRevisions(LastSeqNo);
            config.UpdateAppSettings("Settings", "Last Sequence", SeqNo.ToString());
            LastSeqNo = SeqNo;
            TimerActive = false;
        }
        public static ConfigCls config = new ConfigCls("Settings.ini");
        /// <summary>
        /// Public Constructor for WindowsService.
        /// - Put all of your Initialization code here.
        /// </summary>
        /// 

        public WindowsService2()
        {
            this.ServiceName = "Check Revisions3";
            this.EventLog.Source = "Check Revisions3";
            this.EventLog.Log = "Application";
            
            // These Flags set whether or not to handle that specific
            //  type of event. Set to true if you need it, false otherwise.
            this.CanHandlePowerEvent = true;
            this.CanHandleSessionChangeEvent = true;
            this.CanPauseAndContinue = true;
            this.CanShutdown = true;
            this.CanStop = true;

            
            //if (!EventLog.SourceExists("CheckRevisions"))
            //    EventLog.CreateEventSource("CheckRevisions", "Application");
        }

        /// <summary>
        /// The Main Thread: This is where your Service is Run.
        /// </summary>
        /// 
        static void Init()
        {
            LastSeqNo = config.GetProfileEntry("Settings", "Last Sequence", "");
            Logger.LogActivity("Service Started" + System.Environment.NewLine);
            Logger.LogActivity("Last Sequence: " + LastSeqNo + System.Environment.NewLine);
            var timer = new System.Threading.Timer(
            e => Timer(),
            null,
            TimeSpan.Zero,
            TimeSpan.FromMinutes(1));
        }
        static void Main2()
        {
            
            if (System.Environment.GetCommandLineArgs().Length>0)
                if (System.Environment.GetCommandLineArgs()[1]=="debug") {
                    Init();
                    while (true)
                    {
                        System.Windows.Forms.Application.DoEvents();
                    }
                }
            Logger.LogActivity("Enter main" + System.Environment.NewLine);
            ServiceBase.Run(new WindowsService2());
            Logger.LogActivity("Service ran" + System.Environment.NewLine);
       
        
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
        protected override void OnStart(string[] args)
        {
            base.OnStart(args);
            File.AppendAllText("c:\\CR_test.txt", "2");
            Init();
           
        }

        /// <summary>
        /// OnStop: Put your stop code here
        /// - Stop threads, set final data, etc.
        /// </summary>
        protected override void OnStop()
        {
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
