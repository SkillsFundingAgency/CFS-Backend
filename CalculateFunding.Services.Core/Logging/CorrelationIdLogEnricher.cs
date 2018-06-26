using CalculateFunding.Services.Core.Interfaces.Logging;
using Serilog.Core;
using Serilog.Events;
using System;

namespace CalculateFunding.Services.Core.Logging
{
    /// <summary>
    /// Creates a new instance of CorrelationIdLogEnricher
    /// </summary>
    public class CorrelationIdLogEnricher : ILogEventEnricher
    {
        private ICorrelationIdProvider _correlationIdLookup;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Weir.Synertrex.Common.Logging.CorrelationIdLogEnricher" /> class.
        /// </summary>
        /// <param name="correlationIdLookup">Correlation ID Lookup Provider</param>
        public CorrelationIdLogEnricher(ICorrelationIdProvider correlationIdLookup)
        {
            if (correlationIdLookup == null)
            {
                throw new ArgumentNullException("correlationIdLookup");
            }
            _correlationIdLookup = correlationIdLookup;
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
            string value = _correlationIdLookup.GetCorrelationId();
            if (!string.IsNullOrWhiteSpace(value))
            {
                LogEventProperty property = propertyFactory.CreateProperty("CorrelationId", value, false);
                logEvent.AddOrUpdateProperty(property);
            }
        }
    }
}
