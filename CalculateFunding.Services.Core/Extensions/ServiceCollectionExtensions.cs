using CalculateFunding.Repositories.Common.Cosmos;
using CalculateFunding.Services.Core.Interfaces.Logging;
using CalculateFunding.Services.Core.Interfaces.ServiceBus;
using CalculateFunding.Services.Core.Logging;
using CalculateFunding.Services.Core.Options;
using CalculateFunding.Services.Core.ServiceBus;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using System;
using System.IO;
using System.Reflection;
using System.Text;

namespace CalculateFunding.Services.Core.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddCosmosDb(this IServiceCollection builder, IConfigurationRoot config)
        {
            CosmosDbSettings cosmosDbSettings = new CosmosDbSettings();

            config.Bind("CosmosDbSettings", cosmosDbSettings);

            builder.AddSingleton<CosmosDbSettings>(cosmosDbSettings);

            builder
                .AddScoped<CosmosRepository>();

            return builder;
        }

        public static IServiceCollection AddServiceBus(this IServiceCollection builder, IConfigurationRoot config)
        {
            ServiceBusSettings serviceBusSettings = new ServiceBusSettings();

            config.Bind("ServiceBusSettings", serviceBusSettings);

            builder.AddSingleton<ServiceBusSettings>(serviceBusSettings);

            builder
                .AddScoped<IMessengerService, MessengerService>();

            builder
                .AddScoped<IMessagePumpService, MessagePumpService>();

            return builder;
        }

        public static IServiceCollection AddLogging(this IServiceCollection builder, IConfigurationRoot config, string serviceName)
        {
            ApplicationInsightsOptions appInsightsOptions = new ApplicationInsightsOptions();

            config.Bind("ApplicationInsightsOptions", appInsightsOptions);

            builder.AddSingleton<ApplicationInsightsOptions>(appInsightsOptions);

            builder.AddSingleton<ICorrelationIdProvider, CorrelationIdProvider>();

            builder.AddScoped<Serilog.ILogger>(c => GetLoggerConfiguration(c.GetService<ICorrelationIdProvider>(), appInsightsOptions, serviceName).CreateLogger());

            return builder;
        }

        public static IServiceScope CreateHttpScope(this IServiceProvider serviceProvider, HttpRequest request)
        {
            ICorrelationIdProvider correlationIdProvider = serviceProvider.GetService<ICorrelationIdProvider>();

            var correlationId = request.GetCorrelationId();

            correlationIdProvider.SetCorrelationId(correlationId);

            if(!request.HttpContext.Response.Headers.ContainsKey("sfa-correlationId"))
                request.HttpContext.Response.Headers.Add("sfa-correlationId", correlationId);

            return serviceProvider.CreateScope();
        }

        public static LoggerConfiguration GetLoggerConfiguration(ICorrelationIdProvider correlationIdProvider, ApplicationInsightsOptions options, string serviceName)
        {
            if (correlationIdProvider == null)
            {
                throw new ArgumentNullException(nameof(correlationIdProvider));
            }
            if (options == null)
            {
                throw new ArgumentNullException(nameof(correlationIdProvider));
            }
            string appInsightsKey = options.InstrumentationKey;

            if (string.IsNullOrWhiteSpace(appInsightsKey))
            {
                throw new InvalidOperationException("Unable to lookup Application Insights Configuration key from Configuration Provider. The value returned was empty string");
            }
            return new LoggerConfiguration().Enrich.With(new ILogEventEnricher[]
            {
                new CorrelationIdLogEnricher(correlationIdProvider)
            }).Enrich.With(new ILogEventEnricher[]
            {
                new ServiceNameLogEnricher(serviceName)
            }).WriteTo.ApplicationInsightsTraces(new TelemetryConfiguration
            {
                InstrumentationKey = appInsightsKey,

            }, LogEventLevel.Verbose, null, null);
        }
    }
}
