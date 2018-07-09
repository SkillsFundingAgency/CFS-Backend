﻿using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Models.Specs;
using CalculateFunding.Repositories.Common.Cosmos;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Core.Interfaces.Logging;
using CalculateFunding.Services.Core.Interfaces.Proxies;
using CalculateFunding.Services.Core.Logging;
using CalculateFunding.Services.Core.Options;
using CalculateFunding.Services.Core.Proxies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using System;
using System.Security.Claims;
using CalculateFunding.Models.Results;
using CalculateFunding.Services.Core.Interfaces.ServiceBus;
using CalculateFunding.Services.Core.Interfaces.Caching;
using CalculateFunding.Services.Core.Caching;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Models.Scenarios;
using CalculateFunding.Services.Core.ServiceBus;
using System.Linq;
using CalculateFunding.Models.Users;
using Microsoft.Azure.ServiceBus;
using CalculateFunding.Services.Core.Interfaces.Services;
using CalculateFunding.Services.Core.Services;
using CalculateFunding.Models;

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

        public static IServiceCollection AddCalcsInterServiceClient(this IServiceCollection builder, IConfigurationRoot config)
        {
            builder
                .AddSingleton<ICalcsApiClientProxy, CalcsApiProxy>((ctx)=> {
                    ApiOptions apiOptions = new ApiOptions();

                    config.Bind("calcsClient", apiOptions);

                    ILogger logger = ctx.GetService<ILogger>();
                    ICorrelationIdProvider correlationIdProvider = ctx.GetService<ICorrelationIdProvider>();

                    return new CalcsApiProxy(apiOptions, logger, correlationIdProvider);
                });

            return builder;
        }

        public static IServiceCollection AddScenariosInterServiceClient(this IServiceCollection builder, IConfigurationRoot config)
        {
            builder
                 .AddSingleton<IScenariosApiClientProxy, ScenariosApiProxy>((ctx) => {
                     ApiOptions apiOptions = new ApiOptions();

                     config.Bind("scenariosClient", apiOptions);

                     ILogger logger = ctx.GetService<ILogger>();
                     ICorrelationIdProvider correlationIdProvider = ctx.GetService<ICorrelationIdProvider>();

                     return new ScenariosApiProxy(apiOptions, logger, correlationIdProvider);
                 });

            return builder;
        }

        public static IServiceCollection AddSpecificationsInterServiceClient(this IServiceCollection builder, IConfigurationRoot config)
        {
            builder
                 .AddSingleton<ISpecificationsApiClientProxy, SpecificationsApiProxy>((ctx) => {
                     ApiOptions apiOptions = new ApiOptions();

                     config.Bind("specificationsClient", apiOptions);

                     ILogger logger = ctx.GetService<ILogger>();
                     ICorrelationIdProvider correlationIdProvider = ctx.GetService<ICorrelationIdProvider>();

                     return new SpecificationsApiProxy(apiOptions, logger, correlationIdProvider);
                 });

            return builder;
        }

        public static IServiceCollection AddResultsInterServiceClient(this IServiceCollection builder, IConfigurationRoot config)
        {
            builder
                 .AddSingleton<IResultsApiClientProxy, ResultsApiProxy>((ctx) => {
                     ApiOptions apiOptions = new ApiOptions();

                     config.Bind("resultsClient", apiOptions);

                     ILogger logger = ctx.GetService<ILogger>();
                     ICorrelationIdProvider correlationIdProvider = ctx.GetService<ICorrelationIdProvider>();

                     return new ResultsApiProxy(apiOptions, logger, correlationIdProvider);
                 });

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

            builder
               .AddSingleton<ISearchRepository<ScenarioIndex>, SearchRepository<ScenarioIndex>>();

            builder
              .AddSingleton<ISearchRepository<TestScenarioResultIndex>, SearchRepository<TestScenarioResultIndex>>();

            builder
              .AddSingleton<ISearchRepository<CalculationProviderResultsIndex>, SearchRepository<CalculationProviderResultsIndex>>();

            return builder;
        }

        public static IServiceCollection AddServiceBus(this IServiceCollection builder, IConfigurationRoot config)
        {
            ServiceBusSettings serviceBusSettings = new ServiceBusSettings();

            config.Bind("ServiceBusSettings", serviceBusSettings);

            builder.AddSingleton(serviceBusSettings);

            builder
                .AddSingleton<IMessengerService, MessengerService>();

            return builder;
        }

        public static IServiceCollection AddLogging(this IServiceCollection builder, string serviceName)
        {
            builder.AddSingleton<ICorrelationIdProvider, CorrelationIdProvider>();

            builder.AddSingleton<Serilog.ILogger>((ctx) =>
            {
                TelemetryClient client = ctx.GetService<TelemetryClient>();

              return GetLoggerConfiguration(client, serviceName).CreateLogger();
            });

            //builder.AddSingleton(logger);
            //builder.AddSingleton<Serilog.ILogger>(c => GetLoggerConfiguration(c.GetService<ICorrelationIdProvider>(), appInsightsOptions, serviceName).CreateLogger());

            return builder;
        }

        public static IServiceCollection AddTelemetry(this IServiceCollection builder)
        {
            builder.AddSingleton<ITelemetry, ApplicationInsightsTelemetrySink>((ctx) =>
            {
                TelemetryClient client = ctx.GetService<TelemetryClient>();

                return new ApplicationInsightsTelemetrySink(client);
            });

            return builder;
        }

        public static IServiceCollection AddApplicationInsightsTelemetryClient(this IServiceCollection builder, IConfigurationRoot config)
        {
            Guard.ArgumentNotNull(config, nameof(config));

            ApplicationInsightsOptions appInsightsOptions = new ApplicationInsightsOptions();

            config.Bind("ApplicationInsightsOptions", appInsightsOptions);

         
            string appInsightsKey = appInsightsOptions.InstrumentationKey;

            if (string.IsNullOrWhiteSpace(appInsightsKey))
            {
                throw new InvalidOperationException("Unable to lookup Application Insights Configuration key from Configuration Provider. The value returned was empty string");
            }

            TelemetryClient telemtryClient = new TelemetryClient(new TelemetryConfiguration
            {
                InstrumentationKey = appInsightsKey,

            });

            builder.AddSingleton(telemtryClient);

            return builder;
        }

        public static IServiceScope CreateHttpScope(this IServiceProvider serviceProvider, HttpRequest request)
        {
            ICorrelationIdProvider correlationIdProvider = serviceProvider.GetService<ICorrelationIdProvider>();

            var correlationId = request.GetCorrelationId();

            correlationIdProvider.SetCorrelationId(correlationId);

            if (!request.HttpContext.Response.Headers.ContainsKey("sfa-correlationId"))
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

        public static LoggerConfiguration GetLoggerConfiguration(TelemetryClient telemetryClient, string serviceName)
        {
            Guard.ArgumentNotNull(telemetryClient, nameof(telemetryClient));
            Guard.IsNullOrWhiteSpace(serviceName, nameof(serviceName));

            return new LoggerConfiguration()
            //.Enrich.With(new ILogEventEnricher[]
            //{
            //    new CorrelationIdLogEnricher(correlationIdProvider)
            //})
            .Enrich.With(new ILogEventEnricher[]
            {
                new ServiceNameLogEnricher(serviceName)
            })
            .WriteTo.ApplicationInsightsTraces(telemetryClient, LogEventLevel.Verbose, null, null);
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

        public static IServiceCollection AddEngineSettings(this IServiceCollection builder, IConfigurationRoot config)
        {
            EngineSettings engineSettings = new EngineSettings();

            config.Bind("engineSettings", engineSettings);

            builder.AddSingleton<EngineSettings>(engineSettings);

            return builder;
        }

        public static IServiceCollection AddUserProviderFromMessage(this IServiceCollection builder, Message message)
        {
            builder.AddScoped<IUserProfileProvider, UserProfileProvider>((ctx) =>
            {
                Reference user = message.GetUserDetails();

                UserProfileProvider userProfileProvider = new UserProfileProvider();

                userProfileProvider.SetUser(user.Id, user.Name);

                return userProfileProvider;
            });

            return builder;
        }

        public static IServiceCollection AddUserProviderFromRequest(this IServiceCollection builder)
        {
            builder.AddScoped<IUserProfileProvider, UserProfileProvider>((ctx) =>
            {
                IHttpContextAccessor httpContextAccessor = ctx.GetService<IHttpContextAccessor>();

                string userId = httpContextAccessor.HttpContext?.User?.Claims.FirstOrDefault(m => m.Type == ClaimTypes.Sid)?.Value;

                string userName = httpContextAccessor.HttpContext?.User?.Claims.FirstOrDefault(m => m.Type == ClaimTypes.Name)?.Value;

                UserProfileProvider userProfileProvider = new UserProfileProvider();

                userProfileProvider.SetUser(userId, userName);

                return userProfileProvider;
            });

            return builder;
        }

        public static IServiceCollection AddPolicySettings(this IServiceCollection builder, IConfigurationRoot config)
        {
            PolicySettings policySettings = new PolicySettings();

            config.Bind("policy", policySettings);

            builder.AddSingleton<PolicySettings>(policySettings);

            return builder;
        }
    }
}
