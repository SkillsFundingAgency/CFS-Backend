using System;
using System.Collections.Generic;
using System.Text;
using CalculateFunding.Services.Core.Interfaces.Logging;

namespace CalculateFunding.Services.Core.Logging
{
    public class ConsoleTelemetrySink : ITelemetry
    {
        public void TrackEvent(string eventName, IDictionary<string, string> properties = null, IDictionary<string, double> metrics = null)
        {
            Console.WriteLine("Event: {0}", eventName);
        }
    }
}
