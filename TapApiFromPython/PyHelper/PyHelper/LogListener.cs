using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using OpenTap.Diagnostic;
using System.Windows;

namespace Keysight.OpenTap.Plugins.Python
{
    public partial class PyHelper
    {
        public class LogListener : ILogListener
        {
            public delegate void EventsLoggedCallback(Event[] events);

            public event EventsLoggedCallback PyEventsLogged;

            public LogListener()
            {
                Log.AddListener(this);
            }

            public void EventsLogged(Event[] events)
            {
                PyEventsLogged(events);
                //LogEvents.AddRange(events);
            }

            public void Flush()
            {

            }

        }
    }
}
