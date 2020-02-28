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
    public class eCoreService
    {
    
       
        public static string GetEmployees(string customerId, string password, string vendorKey, string employeeId=null)
        {
            try
            {
                string serviceUri =
                    string.Format("https://services.ecoresoftware.com/API_v1.3/EmployeeService.svc/GetEmployees?custId={0}&pass={1}&vendorKey={2}&" + (employeeId==null?"": "empId ={3}"), customerId, password, vendorKey, employeeId);
                string content = "";
                HttpResponseMessage response = WindowsService.UrlRequest(serviceUri, out content, "GET", content_type: "application/xml");

                EmployeeResponse.Deserialize(content);

                return content;

            }
            catch (Exception ex) { Logger.LogException(ex); return ex.Message; }
        }
        public static string GetSchedules(string customerId, string password, string vendorKey, string employeeId, string starttime, string endtime)
        {
            try
            {
                string serviceUri =
                    string.Format("https://services.ecoresoftware.com/API_v1.3/EmployeeService.svc/GetSchedules?custId={0}&pass={1}&vendorKey={2}&" +"&starttime={4}&endtime={5}" + (employeeId == null ? "" : "empId={3}"), customerId, password, vendorKey, starttime, endtime, employeeId);
                string content = "";
                HttpResponseMessage response = WindowsService.UrlRequest(serviceUri, out content, "GET", content_type: "application/xml");

                EmployeeResponse.Deserialize(content);

                return content;

            }
            catch (Exception ex) { Logger.LogException(ex); return ex.Message; }
        }
        public static string GetPunches(string customerId, string password, string vendorKey, string employeeId, string starttime, string endtime)
        {
            try
            {
                string serviceUri =
                    string.Format("https://services.ecoresoftware.com/API_v1.3/EmployeeService.svc/GetPunches?custId={0}&pass={1}&vendorKey={2}&" + "&starttime={4}&endtime={5}" + (employeeId == null ? "" : "empId={3}"), customerId, password, vendorKey, starttime, endtime, employeeId);
                string content = "";
                HttpResponseMessage response = WindowsService.UrlRequest(serviceUri, out content, "GET", content_type: "application/xml");

                EmployeeResponse.Deserialize(content);

                return content;

            }
            catch (Exception ex) { Logger.LogException(ex); return ex.Message; }
        }

        public static string GetCertifications(string customerId, string password, string vendorKey, string employeeId = null)
        {
            try
            {
                string serviceUri =
                    string.Format("https://services.ecoresoftware.com/API_v1.3/EmployeeService.svc/GetCertifications?custId={0}&pass={1}&vendorKey={2}&" + (employeeId == null ? "" : "empId ={3}"), customerId, password, vendorKey, employeeId);
                string content = "";
                HttpResponseMessage response = WindowsService.UrlRequest(serviceUri, out content, "GET", content_type: "application/xml");

                EmployeeResponse.Deserialize(content);

                return content;

            }
            catch (Exception ex) { Logger.LogException(ex); return ex.Message; }
        }
    }
}
