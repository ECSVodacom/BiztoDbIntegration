using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WriteEventLog
{
    class Program
    {       
        static void Main(string[] args)
        {
            EventLog eventLog = new EventLog { Source = "BizToDBIntegration" };
            if (!System.Diagnostics.EventLog.SourceExists("BizToDBIntegration"))
                System.Diagnostics.EventLog.CreateEventSource("BizToDBIntegration", "Application");

            eventLog.WriteEvent(new EventInstance(1, 0, EventLogEntryType.Error), new string[] { "Test" });            

        }
    }
}
