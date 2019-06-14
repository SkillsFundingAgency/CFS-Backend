using Serilog.Core;
using Serilog.Events;
using System;

namespace CalculateFunding.Services.Core.Logging
{
    public class ServiceNameLogEnricher : ILogEventEnricher
    {
        private string _serviceName;

        public ServiceNameLogEnricher(string serviceName)
        {
            _serviceName = serviceName;
        }

        /// <summary>
        /// Enrich LogEvent message with provided CorrelationId or generate a new one for this HTTP request.
        /// </summary>
        /// <param name="logEvent">Log Event</param>
        /// <param name="propertyFactory">Serilog Property Factory</param>
        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            if (logEvent == null)
            {
                throw new ArgumentNullException(nameof(logEvent));
            }
            if (propertyFactory == null)
            {
                throw new ArgumentNullException(nameof(propertyFactory));
            }
            if (string.IsNullOrWhiteSpace(_serviceName))
            {
                _serviceName = "N/A";
            }
            LogEventProperty property = propertyFactory.CreateProperty("Service", _serviceName, false);
            logEvent.AddOrUpdateProperty(property);
        }
    }
}
