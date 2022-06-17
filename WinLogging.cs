using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeltonikaService
{
    public class WinLogging
    {
        static readonly string LogName = "Teltonika";
        static readonly string SourceName = "TeltonikaService";

        static EventLog objEventLog = new EventLog();
            

        public static void LogEvent(string Message, EventLogEntryType EventType)
        {
            RegisterLog();

            objEventLog.Source = SourceName;
            objEventLog.WriteEntry(Message, EventType);
            objEventLog.Dispose();

        }

        public static void RegisterLog()
        {
            if (!EventLog.SourceExists(SourceName))
            {
                //Register log in windows. Requries admin rights. (Can only be done once with app name, changes require reboot).
                EventLog.CreateEventSource(LogName, SourceName);
            }
        }

    }
}
