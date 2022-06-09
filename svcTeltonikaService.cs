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
            TeltonikaFunctions.LoadConfig(); //Load Configuration info.

            while (shutdown == false)
            {
                //Check messages.
                try
                {
                    TeltonikaFunctions.GetMessageList();
                }
                catch
                {

                }
                //Process outbox.
                try
                {
                    TeltonikaFunctions.ProcessOutbox();
                }
                catch
                {

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
