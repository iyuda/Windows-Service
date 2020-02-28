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
    public class Schedule
    {

        [System.Xml.Serialization.XmlElementAttribute("EmployeeId")]
        public string EmployeeId { get; set; }

        [System.Xml.Serialization.XmlElementAttribute("StartTime")]
        public string StartTime { get; set; }

        [System.Xml.Serialization.XmlElementAttribute("EndTime")]
        public string EndTime { get; set; }

        [System.Xml.Serialization.XmlElementAttribute("Duration")]
        public string Duration { get; set; }

        [System.Xml.Serialization.XmlElementAttribute("Qualification")]
        public string Qualification { get; set; }

        [System.Xml.Serialization.XmlElementAttribute("UnitName")]
        public string UnitName { get; set; }

        [System.Xml.Serialization.XmlElementAttribute("CostCenter")]
        public string CostCenter { get; set; }

        [System.Xml.Serialization.XmlElementAttribute("EarningCode")]
        public string EarningCode { get; set; }

        [System.Xml.Serialization.XmlElementAttribute("ResourceType")]
        public string ResourceType { get; set; }

        [System.Xml.Serialization.XmlElementAttribute("Comments")]
        public string Comments { get; set; }

        [System.Xml.Serialization.XmlElementAttribute("ItemID")]
        public string ItemID { get; set; }


    }
    [XmlRoot("ScheduleResponse", Namespace = "http://schemas.datacontract.org/2004/07/API.Service")]
    public class ScheduleResponse
    {
        //  [XmlArray(IsNullable = true),
        [XmlArray("Schedules", IsNullable = true)]
        [XmlArrayItem("Schedule", typeof(EmployeeProfile))]
        public Schedule[] Schedule { get; set; }
        public static void Deserialize(string content)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(ScheduleResponse));
            using (StringReader reader = new StringReader(content))
            {
                ScheduleResponse Schedule = (ScheduleResponse)serializer.Deserialize(reader);

            }


        }
    }

}
