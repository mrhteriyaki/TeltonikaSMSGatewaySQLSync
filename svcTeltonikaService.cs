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
            bool error_state_in = false;
            bool error_state_out = false;
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
                    error_state_in = false; //clear error state.
                }
                catch (Exception ex)
                {
                    if (error_state_in == false) //only log 1 error.
                    {
                        WinLogging.LogEvent("Get Message list failure exception: " + ex.ToString(), EventLogEntryType.Error);
                        error_state_in = true;
                    }                   
                }
                //Process outbox.
                try
                {
                    TeltonikaFunctions.ProcessOutbox();
                    error_state_out = false;
                }
                catch (Exception ex)
                {
                    if (error_state_out == false)
                    {
                        WinLogging.LogEvent("Outbox processing failure exception: " + ex.ToString(), EventLogEntryType.Error);
                        error_state_out = true;
                    }
                }


                int counter = 0; //delay 1 seconds
                while (counter < 10 && shutdown == false)
                {
                    System.Threading.Thread.Sleep(100);
                    counter++;
                }

            }

        }


    }
}
