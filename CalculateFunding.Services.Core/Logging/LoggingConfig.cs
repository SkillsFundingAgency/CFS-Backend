using Serilog;
using System;
using System.Collections.Generic;
using System.Text;

namespace CalculateFunding.Services.Core.Logging
{
    public static class LoggingConfig
    {
        //public static LoggerConfiguration GetLoggerConfiguration(ICorrelationIdLookup correlationLookup, IConfigurationProvider configProvider)
        //{
        //    if (correlationLookup == null)
        //    {
        //        throw new ArgumentNullException("correlationLookup");
        //    }
        //    if (configProvider == null)
        //    {
        //        throw new ArgumentNullException("configProvider");
        //    }
        //    string configurationValue = configProvider.GetConfigurationValue("ApplicationInsightsKey");
        //    if (string.IsNullOrWhiteSpace(configurationValue))
        //    {
        //        throw new InvalidOperationException("Unable to lookup Application Insights Configuration key from Configuration Provider. The value returned was empty string");
        //    }
        //    return new LoggerConfiguration().Enrich.With(new ILogEventEnricher[]
        //    {
        //        new CorrelationIdLogEnricher(correlationLookup)
        //    }).Enrich.With(new ILogEventEnricher[]
        //    {
        //        new ServiceNameLogEnricher(configProvider)
        //    }).WriteTo.ApplicationInsightsTraces(configurationValue, LogEventLevel.Verbose, null, null);
        //}
    }
}
