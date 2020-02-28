using CemsMobilePcrSrv;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading;
using System.Web;
using System.Windows.Forms;

    public class Fax_Email
    {


    public static string sendErrorEmail( string email_to, string body)
    {
        var fromAddress = new MailAddress("support@creativeems.com", "From Creative");

        const string fromPassword = "support4732.";
        string subject = "Mobile Pcr Service Error";

        var smtp = new SmtpClient
        {
            Host = "smtp.gmail.com",
            Port = 587,
            EnableSsl = true,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            UseDefaultCredentials = false,
            Credentials = new NetworkCredential(fromAddress.Address, fromPassword)
        };
        var message = new MailMessage();
        message.From = fromAddress;
        foreach (string address in email_to.Split(';'))
        {
            message.To.Add(address);
        }
        message.Subject = subject;
        message.Body = body;

        try
        {
            smtp.Send(message);
            Logger.LogActivity("E-Mail was sent to " + email_to);
            Console.WriteLine("E-mail successfully sent.");
            return "OK";
        }
        catch (Exception ex)
        {
            //File.AppendAllText(Directory.GetCurrentDirectory() + "FaxlogError.txt", DateTime.Now.ToString() + " Error sending fax for: " + PhoneNumber + "\r\n");
            Logger.LogException(ex);
            Console.WriteLine("Failure Sending e-mail.");
            return ex.Message;
        }

        //message.Dispose();
    }
    public static string sendEmailFax(string Filename, string PhoneNumber, string FromAgencyName)
        {
            var fromAddress = new MailAddress("support@creativeems.com", "From Creative Fax");
            string FaxTo = string.Format("{0}@metrofax.com", PhoneNumber);
            var toAddress = new MailAddress(FaxTo, "To Facility");
            const string fromPassword = "support4732.";
            string subject = "Run Sheet From " + FromAgencyName;// this is RE in email
            const string body = @"The document(s) accompanying this fax contain(s) confidential information which is legally privileged. The information is intended only for the use of the intended recipient/institution named above. If you are not the intended recipient you are hereby notified that any reading, disclosure, copying, distribution or taking of any action in reliance on the contents of the telecopied information except in direct delivery to the intended recipient names above is strictly prohibited. If you have received this fax in error, please notify us immediately by the above phone number to arrange proper disposal of the original documents. ";

            var smtp = new SmtpClient
            {
                Host = "smtp.gmail.com",
                Port = 587,
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(fromAddress.Address, fromPassword)
            };
            var message = new MailMessage(fromAddress, toAddress);
            message.Subject = subject;
            message.Body = body;
            message.Attachments.Add(new System.Net.Mail.Attachment(Filename));
            //  message.BodyEncoding = System.Text.Encoding.UTF8;
            //  message.DeliveryNotificationOptions = DeliveryNotificationOptions.OnSuccess;
            try
            {
                smtp.Send(message);
                Console.WriteLine("Fax successfully sent.");
                return "OK";
            }
            catch (Exception ex)
            {
                //File.AppendAllText(Directory.GetCurrentDirectory() + "FaxlogError.txt", DateTime.Now.ToString() + " Error sending fax for: " + PhoneNumber + "\r\n");
                Logger.LogException(ex);
                Console.WriteLine("Failure Sending Fax.");
                return ex.Message;
            }

            message.Dispose();
        }

        public static bool SendTestEmailFax(string Filename, string PhoneNumber, string FromAgencyName, MailAddress fromAddress, MailAddress toAddress, string fromPassword)
        {

            string subject = "Run Sheet From " + FromAgencyName;// this is RE in email
            const string body = @"The document(s) accompanying this fax contain(s) confidential information which is legally privileged. The information is intended only for the use of the intended recipient/institution named above. If you are not the intended recipient you are hereby notified that any reading, disclosure, copying, distribution or taking of any action in reliance on the contents of the telecopied information except in direct delivery to the intended recipient names above is strictly prohibited. If you have received this fax in error, please notify us immediately by the above phone number to arrange proper disposal of the original documents. ";

            var smtp = new SmtpClient
            {
                Host = "smtp.gmail.com",
                Port = 587,
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(fromAddress.Address, fromPassword)
            };
            var message = new MailMessage(fromAddress, toAddress);
            message.Subject = subject;
            message.Body = body;
            message.Attachments.Add(new System.Net.Mail.Attachment(Filename));
            //  message.BodyEncoding = System.Text.Encoding.UTF8;
            //  message.DeliveryNotificationOptions = DeliveryNotificationOptions.OnSuccess;
            try
            {
                smtp.Send(message);
                Console.WriteLine("Fax successfully sent.");
                return true;
            }
            catch (Exception)
            {
                File.AppendAllText(Directory.GetCurrentDirectory() + "FaxlogError.txt", DateTime.Now.ToString() + " Fax successfully sent for " + PhoneNumber + "\r\n");
                Console.WriteLine("Failure Sending Fax.");
                return false;
            }

            message.Dispose();
        }



        


        protected void Capture(object sender, EventArgs e)
        {
               
        }

        private void DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            //WebBrowser browser = sender as WebBrowser;
            //using (Bitmap bitmap = new Bitmap(browser.Width, browser.Height))
            //{
            //     browser.DrawToBitmap(bitmap, new Rectangle(0, 0, browser.Width, browser.Height));
            //     using (MemoryStream stream = new MemoryStream())
            //     {
            //          bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
            //          byte[] bytes = stream.ToArray();
            //          imgScreenShot.Visible = true;
            //          imgScreenShot.ImageUrl = "data:image/png;base64," + Convert.ToBase64String(bytes);
            //     }
            //}
        }
       
        public string CreateFile(string pcr_id, string UrlOfReport)
        {
            string FileName="";
            Thread thread = new Thread(delegate()
            {
                using (WebBrowser browser = new WebBrowser())
                {
                        browser.ScrollBarsEnabled = false;
                        browser.AllowNavigation = true;
                        browser.Navigate(UrlOfReport);
                        browser.Width = 1024;
                        browser.Height = 768;
                        //browser.DocumentCompleted += new WebBrowserDocumentCompletedEventHandler(DocumentCompleted);
                        while (browser.ReadyState != WebBrowserReadyState.Complete)
                        {
                            System.Windows.Forms.Application.DoEvents();
                        }      
                }

            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();

            string FaxDirectory = "c:\\Creativeems\\Fax\\";
            if (!Directory.Exists(FaxDirectory)) Directory.CreateDirectory(FaxDirectory);
            FileName = FaxDirectory + "\\PcrReport_" + pcr_id + ".pdf";
            if (File.Exists(FileName)) File.Delete(FileName);
            try
            {
                WebClient client = new WebClient();
                Uri uri = new Uri("http://localhost/cemslocal/yiicems/Reports/ReportFiles/PcrReport_" + pcr_id + ".pdf");
                client.DownloadFile(uri, FileName);
            }
            catch { }
            if (File.Exists(FileName))
                return (FileName);
            else
                return (null);
            //if (!UseIReport)
            //{
            //string PageSize = "7in*8in";
            //string argument1 = "rasterize.js";
            //string Path = @"phantomjs.exe";
            //string argument = string.Format(@"{0} ""{1}"" ""{2}""", argument1, UrlOfReport, PdfFilename);
            //Process p = new Process();
            //p.StartInfo = new ProcessStartInfo(Path, " --ignore-ssl-errors=yes " + argument);
            //p.StartInfo.CreateNoWindow = true;
            //p.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            //p.StartInfo.UseShellExecute = false;
            //p.Start();
            //p.WaitForExit();
            //}
            //else
            //{
            //     string Path = @"Reports.exe";
            //     string argument = string.Format(@"{0} ""{1}"" ""{2}""", argument1, UrlOfReport, PdfFilename);
            //}

              

        }
        public static string Geturl_test(string ReportType)
        {
            if (ReportType == "pcr" || ReportType == "pcr_Addendum")
            {
                string Onlinepath = @"https://www.creativeemstest.com/cems/cemslocal/yiicems/index.php?r=Central/Report&pcrid=";
                return Onlinepath;
            }
            if (ReportType == "pcs" || ReportType == "pcs_Addendum")
            {
                string Onlinepath = @"https://www.creativeemstest.com/cems/cemslocal/yiicems/index.php?r=Central/ApcfReport&pcrid=";
                return Onlinepath;
            }
            if (ReportType == "aob" || ReportType == "aob_Addendum")
            {
                string Onlinepath = @"https://www.creativeemstest.com/cems/cemslocal/yiicems/index.php?r=Central/HipaaReport&pcrid=";
                return Onlinepath;
            }
            if (ReportType == "rma" || ReportType == "rma_Addendum")
            {
                string Onlinepath = @"https://www.creativeemstest.com/cems/cemslocal/yiicems/index.php?r=Central/RmaReport&pcrid=";
                return Onlinepath;
            }
            if (ReportType == "total")
            {
                string Onlinepath = @"https://www.creativeemstest.com/cems/cemslocal/yiicems/index.php?r=Central/RmaReport&pcrid=";
                return Onlinepath;
            }

            return "";
        }

        public static string Geturl(string ReportType)
        {
            if (ReportType == "pcr" || ReportType == "pcr_Addendum")
            {
                string Onlinepath = @"https://www.creativeems.com/cems/cemslocal/yiicems/index.php?r=Central/Report&pcrid=";
                return Onlinepath;
            }
            if (ReportType == "pcs" || ReportType == "pcs_Addendum")
            {
                string Onlinepath = @"https://www.creativeems.com/cems/cemslocal/yiicems/index.php?r=Central/ApcfReport&pcrid=";
                return Onlinepath;
            }
            if (ReportType == "aob" || ReportType == "aob_Addendum")
            {
                string Onlinepath = @"https://www.creativeems.com/cems/cemslocal/yiicems/index.php?r=Central/HipaaReport&pcrid=";
                return Onlinepath;
            }
            if (ReportType == "rma" || ReportType == "rma_Addendum")
            {
                string Onlinepath = @"https://www.creativeems.com/cems/cemslocal/yiicems/index.php?r=Central/RmaReport&pcrid=";
                return Onlinepath;
            }
            if (ReportType == "total")
            {
                string Onlinepath = @"https://www.creativeems.com/cems/cemslocal/yiicems/index.php?r=Central/RmaReport&pcrid=";
                return Onlinepath;
            }

            return "";
        }
}