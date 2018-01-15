using CalculateFunding.Services.Core.Options;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using System;

namespace CalculateFunding.Services.Core.Logging
{
    public class ApplicationInsightsService : ILoggingService
    {
        TelemetryClient _telemetryClient;
        string _correlationId = Guid.NewGuid().ToString();

        readonly ApplicationInsightsOptions _appInsightsOptions;

        public ApplicationInsightsService(ApplicationInsightsOptions appInsightsOptions)
        {
            _appInsightsOptions = appInsightsOptions;
        }

        TelemetryClient TelemetryClient
        {
            get
            {
                if(_telemetryClient == null)
                {
                    _telemetryClient = new TelemetryClient
                    {
                        InstrumentationKey = _appInsightsOptions.InstrumentationKey
                    };
                }

                return _telemetryClient;
            }
        }

        public string CorrelationId
        {
            get
            {
                return _correlationId;
            }
            set
            {
                if (!string.IsNullOrWhiteSpace(value))
                    _correlationId = value;
            }
        }

        public void Trace(string message)
        {
            var traceTelmetry = new TraceTelemetry
            {
                Message = message,
                SeverityLevel = SeverityLevel.Information
            };

            traceTelmetry.Context.Operation.Id = _correlationId;

            TelemetryClient.TrackTrace(traceTelmetry);
        }

        public void Exception(string message, Exception exception)
        {
            var exceptionTelmetry = new ExceptionTelemetry
            {
                Message = message,
                SeverityLevel = SeverityLevel.Error,
                Exception = exception
            };

            exceptionTelmetry.Context.Operation.Id = _correlationId;

            TelemetryClient.TrackException(exceptionTelmetry);
        }

        public void FatalException(string message, Exception exception)
        {
            var exceptionTelmetry = new ExceptionTelemetry
            {
                Message = message,
                SeverityLevel = SeverityLevel.Critical,
                Exception = exception
            };

            exceptionTelmetry.Context.Operation.Id = _correlationId;

            TelemetryClient.TrackException(exceptionTelmetry);
        }


        //Add more logs here

    }
}
