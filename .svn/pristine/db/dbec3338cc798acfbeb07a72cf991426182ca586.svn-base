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
    public class EmployeeProfile
    {
        [System.Xml.Serialization.XmlElementAttribute("EmployeeId")]
        public string EmployeeId { get; set; }

        [System.Xml.Serialization.XmlElementAttribute("FirstName")]
        public string FirstName { get; set; }

        [System.Xml.Serialization.XmlElementAttribute("LastName")]
        public string LastName { get; set; }

        [System.Xml.Serialization.XmlElementAttribute("HireDate")]
        public string HireDate { get; set; }

        [System.Xml.Serialization.XmlElementAttribute("FTHireDate")]
        public string FTHireDate { get; set; }

        [System.Xml.Serialization.XmlElementAttribute("PayRate")]
        public string PayRate { get; set; }

        [System.Xml.Serialization.XmlElementAttribute("PayType")]
        public string PayType { get; set; }

        [System.Xml.Serialization.XmlElementAttribute("Address")]
        public string Address { get; set; }

        [System.Xml.Serialization.XmlElementAttribute("City")]
        public string City { get; set; }

        [System.Xml.Serialization.XmlElementAttribute("State")]
        public string State { get; set; }

        [System.Xml.Serialization.XmlElementAttribute("Zip")]
        public string Zip { get; set; }

        [System.Xml.Serialization.XmlElementAttribute("HomePhone")]
        public string HomePhone { get; set; }

        [System.Xml.Serialization.XmlElementAttribute("Pager")]
        public string Pager { get; set; }

        [System.Xml.Serialization.XmlElementAttribute("CellPhone")]
        public string CellPhone { get; set; }

        [System.Xml.Serialization.XmlElementAttribute("EmergencyContactPhone")]
        public string EmergencyContactPhone { get; set; }

        [System.Xml.Serialization.XmlElementAttribute("EmergencyContactPerson")]
        public string EmergencyContactPerson { get; set; }

        [System.Xml.Serialization.XmlElementAttribute("EmailAddress")]
        public string EMailAddress { get; set; }

        [System.Xml.Serialization.XmlElementAttribute("HomeCostCenter")]
        public string HomeCostCenter { get; set; }



    }
    [XmlRoot("EmployeeResponse", Namespace = "http://schemas.datacontract.org/2004/07/API.Service")]
    public class EmployeeResponse
    {
        //  [XmlArray(IsNullable = true),
        [XmlArray("Employees", IsNullable = true)]
        [XmlArrayItem("Employee", typeof(EmployeeProfile))]
        public EmployeeProfile[] EmployeeProfile { get; set; }
        public static void Deserialize(string content)
        {
            // XmlSerializer serializer = new XmlSerializer(typeof(EmployeeProfile[]));
            //var xmlObject = serializer.Deserialize(new StringReader(content));
            //EmployeeProfile[] employeeProfile = (Employe+eProfile[])xmlObject;
            //serializer = new XmlSerializer(typeof(EmployeeProfile[]));
            //content = File.ReadAllText("c:\\temp\\employees.txt");
            XmlSerializer serializer = new XmlSerializer(typeof(EmployeeResponse));
            using (StringReader reader = new StringReader(content))
            {
                EmployeeResponse EmployeeProfiles = (EmployeeResponse)serializer.Deserialize(reader);

                //
                ////StringReader reader = new StringReader(content);
                //EmployeeProfiles = (EmployeeProfileCollection)serializer.Deserialize(new StringReader(content));
            }

            //reader.Close();
        }
    }
   
}
