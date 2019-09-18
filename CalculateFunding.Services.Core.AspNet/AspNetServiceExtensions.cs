using System;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core.Logging;
using CalculateFunding.Services.Core.Options;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CalculateFunding.Services.Core.AspNet
{
    public static class AspNetServiceExtensions
    {
        public static IServiceCollection AddApplicationInsightsForFunctionApps(this IServiceCollection builder, IConfiguration config, string serviceName)
        {
            Guard.ArgumentNotNull(config, nameof(config));

            ApplicationInsightsOptions appInsightsOptions = new ApplicationInsightsOptions();

            config.Bind("ApplicationInsightsOptions", appInsightsOptions);

            string appInsightsKey = appInsightsOptions.InstrumentationKey;

            if (string.IsNullOrWhiteSpace(appInsightsKey))
            {
                throw new InvalidOperationException("Unable to lookup Application Insights Configuration key from Configuration Provider. The value returned was empty string");
            }

            ServiceNameTelemetryInitializer serviceNameEnricher = new ServiceNameTelemetryInitializer(serviceName);

            builder.AddSingleton<ITelemetryInitializer>(serviceNameEnricher);

            // Add call to configure app insights, in order to have ITelemetryInitializer registered before calling
            // as per https://github.com/Microsoft/ApplicationInsights-aspnetcore/wiki/Custom-Configuration
            //builder.AddApplicationInsightsTelemetry(appInsightsOptions.InstrumentationKey);

            return builder;
        }

        public static IServiceCollection AddApplicationInsightsForApiApp(this IServiceCollection builder, IConfiguration config, string serviceName)
        {
            Guard.ArgumentNotNull(config, nameof(config));

            ApplicationInsightsOptions appInsightsOptions = new ApplicationInsightsOptions();

            config.Bind("ApplicationInsightsOptions", appInsightsOptions);

            string appInsightsKey = appInsightsOptions.InstrumentationKey;

            if (string.IsNullOrWhiteSpace(appInsightsKey))
            {
                throw new InvalidOperationException("Unable to lookup Application Insights Configuration key from Configuration Provider. The value returned was empty string");
            }

            ServiceNameTelemetryInitializer serviceNameEnricher = new ServiceNameTelemetryInitializer(serviceName);

            builder.AddSingleton<ITelemetryInitializer>(serviceNameEnricher);

            // Add call to configure app insights, in order to have ITelemetryInitializer registered before calling
            // as per https://github.com/Microsoft/ApplicationInsights-aspnetcore/wiki/Custom-Configuration
            builder.AddApplicationInsightsTelemetry(appInsightsOptions.InstrumentationKey);

            return builder;
        }
    }
}
