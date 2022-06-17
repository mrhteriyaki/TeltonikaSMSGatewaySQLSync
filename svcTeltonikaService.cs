using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace TeltonikaService
{
    public partial class svcTeltonikaService : ServiceBase
    {
        static bool shutdown = false;

        public svcTeltonikaService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {


            Thread ProcThread = new Thread(ProcessLoop);
            ProcThread.Start();

        }

        protected override void OnStop()
        {
            shutdown = true;
        }


        void ProcessLoop()
        {
            bool loadok = false;
            do
            {
                try
                {
                    TeltonikaFunctions.LoadConfig(); //Load Configuration info.
                    WinLogging.LogEvent("Load Config OK", EventLogEntryType.Information);
                    loadok = true;
                }
                catch (Exception ex)
                {
                    WinLogging.LogEvent("Load Config Failure", EventLogEntryType.Warning);
                    Thread.Sleep(500);
                }
            } while (loadok == false && shutdown == false);
            

            while (shutdown == false)
            {
                //Check messages.
                try
                {
                    TeltonikaFunctions.GetMessageList();
                }
                catch (Exception ex)
                {
                    WinLogging.LogEvent("Get Message list failure exception: " + ex.ToString(), EventLogEntryType.Error);
                }
                //Process outbox.
                try
                {
                    TeltonikaFunctions.ProcessOutbox();
                }
                catch (Exception ex)
                {
                    WinLogging.LogEvent("Outbox processing failure exception: " + ex.ToString(), EventLogEntryType.Error);
                }


                int counter = 0; //delay 15 seconds
                while (counter < 150 && shutdown == false)
                {
                    System.Threading.Thread.Sleep(100);
                    counter++;
                }

            }

        }


    }
}
