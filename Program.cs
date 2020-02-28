﻿using System;
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
//using WindowsService;
namespace CemsMobilePcrSrv
{
    class Program
    {
        static string base_url = "http://192.168.1.60:5984/pcr-test";
        static bool TimerActive=false;
        public static string UrlRequest(string url, string method="POST", string content_type="application/json") {
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
                string url = base_url + "/_changes"+ (LastSeqNo==""?"": "?since=" + LastSeqNo);
                string Json1 = UrlRequest(url);
                

                //url = base_url + "?since=" + NewSeqNo;
                //string Json2 = UrlRequest(url, "POST", "application/json");
                
                var SeqArray = (IEnumerable<JToken>)JsonMaker.GetIOSJsonExtract("$.results", Json1);
                if (SeqArray != null) {
                    foreach (JToken SeqToken in SeqArray)
                    {
                        int CurrentSeqNo;
                        int.TryParse(JsonMaker.GetIOSJsonExtract("$.seq", SeqToken).ToString().Split('-')[0], out CurrentSeqNo);
                        //if (CurrentSeqNo == null) continue;
                        //if (CurrentSeqNo > NewSeqNo || CurrentSeqNo <= LastSeqNo) continue;

                        string OutString = "seq: " + CurrentSeqNo + System.Environment.NewLine;
                        string id = JsonMaker.GetIOSJsonExtract("$.id", SeqToken).ToString();
                        OutString+="id: " + id + System.Environment.NewLine;
                        var ChangesArray = (IEnumerable<JToken>)JsonMaker.GetIOSJsonExtract("$.changes", SeqToken.ToString());
                        if (ChangesArray != null) {
                            string rev = JsonMaker.GetIOSJsonExtract("$.rev", ChangesArray.First()).ToString();
                            OutString += "rev: " + rev + System.Environment.NewLine;
                        }
                        OutString += System.Environment.NewLine;
                        OutString += UrlRequest(base_url + "/" + id, "GET") + System.Environment.NewLine;
                        Logger.LogActivity(OutString);
                        string PostJson =Newtonsoft.Json.JsonConvert.SerializeObject(Json1);
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
        static string LastSeqNo="";
        static void Timer()
        {
            File.AppendAllText("c:\\CR_test.txt", "1");
            if (TimerActive) return;
            TimerActive = true;
            System.Windows.Forms.Application.DoEvents();
            string SeqNo = CheckRevisions(LastSeqNo);
            config.UpdateAppSettings("Settings", "Last Sequence", SeqNo);
            if (LastSeqNo == SeqNo)
            {
                Logger.LogActivity("No Changes Were Made!");
            }
            LastSeqNo = SeqNo;
            TimerActive = false;
        }
        public static ConfigCls config = new ConfigCls("Settings.config");
        static void Main1(string[] args)
        {
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                new WindowsService()
            };
            ServiceBase.Run(ServicesToRun);

            LastSeqNo = config.GetProfileEntry("Settings", "Last Sequence", "");
            Logger.LogActivity("Service Started" + System.Environment.NewLine);
            Logger.LogActivity("Last Sequence: " + LastSeqNo + System.Environment.NewLine);
            var timer = new System.Threading.Timer(
            e => Timer(),
            null,
            TimeSpan.Zero,
            TimeSpan.FromMinutes(1));

            while (true)
            {
                System.Windows.Forms.Application.DoEvents();
            }
            //int LastSeqNo = 0;
            //while (true)
            //{
            //    System.Windows.Forms.Application.DoEvents();
            //    int SeqNo = CheckRevisions(LastSeqNo);
            //    LastSeqNo = SeqNo;
            //}
        }
    }
}
