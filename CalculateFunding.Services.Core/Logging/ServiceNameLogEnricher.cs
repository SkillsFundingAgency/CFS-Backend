using Microsoft.Extensions.Configuration;
using Serilog.Core;
using Serilog.Events;
using System;

namespace CalculateFunding.Services.Core.Logging
{
    /// <summary>
    /// Creates a new instance of CorrelationIdLogEnricher
    /// </summary>
    public class ServiceNameLogEnricher : ILogEventEnricher
    {
        private IConfigurationProvider configProvider;

        private string serviceName;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Weir.Synertrex.Common.Logging.ServiceNameLogEnricher" /> class.
        /// </summary>
        /// <param name="configProvider">Correlation ID Lookup Provider</param>
        public ServiceNameLogEnricher(IConfigurationProvider configProvider)
        {
            if (configProvider == null)
            {
               // throw new ArgumentNullException("configProvider");
            }
            this.configProvider = configProvider;
        }

        /// <summary>
        /// Enrich LogEvent message with provided CorrelationId or generate a new one for this HTTP request.
        /// </summary>
        /// <param name="logEvent">&gt;Log Event</param>
        /// <param name="propertyFactory">Serilog Property Factory</param>
        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            if (logEvent == null)
            {
                throw new ArgumentNullException("logEvent");
            }
            if (propertyFactory == null)
            {
                throw new ArgumentNullException("propertyFactory");
            }
            if (string.IsNullOrWhiteSpace(this.serviceName))
            {
                this.serviceName = "specs";
                if (string.IsNullOrWhiteSpace(this.serviceName))
                {
                    this.serviceName = "N/A";
                }
            }
            LogEventProperty property = propertyFactory.CreateProperty("Service", this.serviceName, false);
            logEvent.AddOrUpdateProperty(property);
        }
    }
}
