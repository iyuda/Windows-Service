using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace WindowsService1
{
    public partial class Service1 : ServiceBase
    {
        public Service1()
        {
            InitializeComponent();
            this.ServiceName = "CheckRevisions";
            this.EventLog.Log = "Application";
            
            //// These Flags set whether or not to handle that specific
            ////  type of event. Set to true if you need it, false otherwise.
            //this.CanHandlePowerEvent = true;
            //this.CanHandleSessionChangeEvent = true;
            //this.CanPauseAndContinue = true;
            //this.CanShutdown = true;
            //this.CanStop = true;
        }

        protected override void OnStart(string[] args)
        {

        }

        protected override void OnStop()
        {
        }
    }
}
