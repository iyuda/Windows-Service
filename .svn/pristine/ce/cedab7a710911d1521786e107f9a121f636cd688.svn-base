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
using System.Xml.Serialization;
using System.Xml;

namespace CemsMobilePcrSrv
{
    [Serializable()]
    public class Punch
    {
        [System.Xml.Serialization.XmlElementAttribute("EmployeeId")]
        public string EmployeeId { get; set; }

        [System.Xml.Serialization.XmlElementAttribute("StartTime")]
        public string InPunchTime { get; set; }

        [System.Xml.Serialization.XmlElementAttribute("EndTime")]
        public string OutPunchTime { get; set; }
    }
    [XmlRoot("PunchResponse", Namespace = "http://schemas.datacontract.org/2004/07/API.Service")]
    public class PunchResponse
    {

        [XmlArray("Punches", IsNullable = true)]
        [XmlArrayItem("Punch", typeof(EmployeeProfile))]
        public Punch[] Punch { get; set; }
        public static void Deserialize(string content)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(ScheduleResponse));
            using (StringReader reader = new StringReader(content))
            {
                PunchResponse Punch = (PunchResponse)serializer.Deserialize(reader);

            }


        }
    }

}
