using CalculateFunding.Repositories.Common.Cosmos;
using CalculateFunding.Services.Core.Interfaces.Logging;
using CalculateFunding.Services.Core.Logging;
using CalculateFunding.Services.Core.Options;
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

        public static IServiceCollection AddLogging(this IServiceCollection builder, IConfigurationRoot config, string serviceName)
        {
            //builder
            //    .AddScoped<ILoggingService, ApplicationInsightsService>();

            ApplicationInsightsOptions appInsightsOptions = new ApplicationInsightsOptions();

            config.Bind("ApplicationInsightsOptions", appInsightsOptions);

            builder.AddSingleton<ApplicationInsightsOptions>(appInsightsOptions);

            //builder.AddSingleton(new LoggerFactory()
            //       .AddConsole()
            //       .AddSerilog()
            //        .AddDebug())
            //    .AddLogging();

            builder.AddSingleton<ICorrelationIdProvider, CorrelationIdProvider>();

            builder.AddScoped<Serilog.ILogger>(c => GetLoggerConfiguration(c.GetService<ICorrelationIdProvider>(), appInsightsOptions, serviceName).CreateLogger());

            return builder;
        }

        public static IServiceScope CreateHttpScope(this IServiceProvider serviceProvider, HttpRequest request)
        {
            ICorrelationIdProvider correlationIdProvider = serviceProvider.GetService<ICorrelationIdProvider>();

            var correlationId = request.GetCorrelationId();

            correlationIdProvider.SetCorrelationId(correlationId);

            return serviceProvider.CreateScope();
        }

        public static LoggerConfiguration GetLoggerConfiguration(ICorrelationIdProvider correlationLookup, ApplicationInsightsOptions options, string serviceName)
        {
            if (correlationLookup == null)
            {
                throw new ArgumentNullException("correlationLookup");
            }
            if (options == null)
            {
                throw new ArgumentNullException("options");
            }
            string configurationValue = options.InstrumentationKey;

            if (string.IsNullOrWhiteSpace(configurationValue))
            {
                throw new InvalidOperationException("Unable to lookup Application Insights Configuration key from Configuration Provider. The value returned was empty string");
            }
            return new LoggerConfiguration().Enrich.With(new ILogEventEnricher[]
            {
                new CorrelationIdLogEnricher(correlationLookup)
            }).Enrich.With(new ILogEventEnricher[]
            {
                new ServiceNameLogEnricher(serviceName)
            }).WriteTo.ApplicationInsightsTraces(configurationValue, LogEventLevel.Verbose, null, null);
        }
    }
}
