using System.Collections.Generic;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core.Interfaces.Logging;
using Microsoft.ApplicationInsights;

namespace CalculateFunding.Services.Core.Logging
{
    public class ApplicationInsightsTelemetrySink : ITelemetry
    {
        private readonly TelemetryClient _client;

        public ApplicationInsightsTelemetrySink(TelemetryClient client)
        {
            Guard.ArgumentNotNull(client, nameof(client));
            _client = client;
        }

        public void TrackEvent(string eventName, IDictionary<string, string> properties = null, IDictionary<string, double> metrics = null)
        {
            _client.TrackEvent(eventName, properties, metrics);
        }
    }
}
