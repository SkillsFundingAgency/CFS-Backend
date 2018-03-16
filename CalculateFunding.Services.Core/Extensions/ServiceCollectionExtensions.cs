using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Models.Specs;
using CalculateFunding.Repositories.Common.Cosmos;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Core.Interfaces.Logging;
using CalculateFunding.Services.Core.Interfaces.Proxies;
using CalculateFunding.Services.Core.Logging;
using CalculateFunding.Services.Core.Options;
using CalculateFunding.Services.Core.Proxies;
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
using System.Security.Claims;
using System.Text;
using CalculateFunding.Models.Results;
using CalculateFunding.Services.Core.EventHub;
using CalculateFunding.Services.Core.Interfaces.EventHub;
using CalculateFunding.Services.Core.Interfaces.Caching;
using CalculateFunding.Services.Core.Caching;

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
                .AddSingleton<CosmosRepository>();

            return builder;
        }

        public static IServiceCollection AddInterServiceClient(this IServiceCollection builder, IConfigurationRoot config)
        {
            ApiOptions apiOptions = new ApiOptions();

            config.Bind("apiOptions", apiOptions);

            builder.AddSingleton<ApiOptions>(apiOptions);

            builder
                .AddSingleton<IApiClientProxy, ApiClientProxy>();

            builder
                .AddScoped<IHttpClient, HttpClientProxy>();

            return builder;
        }

        public static IServiceCollection AddSearch(this IServiceCollection builder, IConfigurationRoot config)
        {
            SearchRepositorySettings searchSettings = new SearchRepositorySettings
            {
                SearchServiceName = config.GetValue<string>("SearchServiceName"),
                SearchKey = config.GetValue<string>("SearchServiceKey")
            };

            builder.AddSingleton<SearchRepositorySettings>(searchSettings);

            builder
                .AddSingleton<ISearchRepository<CalculationIndex>, SearchRepository<CalculationIndex>>();

            builder
              .AddSingleton<ISearchRepository<DatasetIndex>, SearchRepository<DatasetIndex>>();

            builder
              .AddSingleton<ISearchRepository<SpecificationIndex>, SearchRepository<SpecificationIndex>>();

	        builder
		        .AddSingleton<ISearchRepository<ProviderIndex>, SearchRepository<ProviderIndex>>();

			return builder;
        }

        public static IServiceCollection AddEventHub(this IServiceCollection builder, IConfigurationRoot config)
        {
            EventHubSettings eventHubSettings = new EventHubSettings();

            config.Bind("EventHubSettings", eventHubSettings);

            builder.AddSingleton(eventHubSettings);

            builder
                .AddScoped<IMessengerService, MessengerService>();

            return builder;
        }

        public static IServiceCollection AddHttpEventHub(this IServiceCollection builder, IConfigurationRoot config)
        {
            EventHubSettings eventHubSettings = new EventHubSettings();

            config.Bind("EventHubSettings", eventHubSettings);

            builder.AddSingleton(eventHubSettings);

            builder
                .AddScoped<IMessengerService, HttpMessengerService>();

            return builder;
        }

        public static IServiceCollection AddLogging(this IServiceCollection builder, IConfigurationRoot config, string serviceName)
        {
            ApplicationInsightsOptions appInsightsOptions = new ApplicationInsightsOptions();

            config.Bind("ApplicationInsightsOptions", appInsightsOptions);

            builder.AddScoped<ICorrelationIdProvider, CorrelationIdProvider>();

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

            string userId = "unknown";
            string username = "unknown";

            if (request.HttpContext.Request.Headers.ContainsKey("sfa-userid"))
                userId = request.HttpContext.Request.Headers["sfa-userid"];

            if (request.HttpContext.Request.Headers.ContainsKey("sfa-username"))
                username = request.HttpContext.Request.Headers["sfa-username"];

            request.HttpContext.User = new ClaimsPrincipal(new[]
            {
                new ClaimsIdentity(new []{ new Claim(ClaimTypes.Sid, userId), new Claim(ClaimTypes.Name, username) })
            });

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

        public static IServiceCollection AddCaching(this IServiceCollection builder, IConfigurationRoot config)
        {
            RedisSettings redisSettings = new RedisSettings();

            config.Bind("redisSettings", redisSettings);

            builder.AddSingleton<RedisSettings>(redisSettings);

            builder
                .AddSingleton<ICacheProvider, StackExchangeRedisClientCacheProvider>();

            return builder;
        }
    }
}
