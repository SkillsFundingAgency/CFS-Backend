using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using CalculateFunding.Common.ApiClient;
using CalculateFunding.Common.ApiClient.Jobs;
using CalculateFunding.Common.Caching;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.FeatureToggles;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Models.Datasets.Schema;
using CalculateFunding.Models.Results.Search;
using CalculateFunding.Models.Scenarios;
using CalculateFunding.Models.Specs;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Core.Interfaces.Logging;
using CalculateFunding.Services.Core.Interfaces.Proxies;
using CalculateFunding.Services.Core.Interfaces.ServiceBus;
using CalculateFunding.Services.Core.Interfaces.Services;
using CalculateFunding.Services.Core.Logging;
using CalculateFunding.Services.Core.Options;
using CalculateFunding.Services.Core.Proxies;
using CalculateFunding.Services.Core.ServiceBus;
using CalculateFunding.Services.Core.Services;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace CalculateFunding.Services.Core.Extensions
{
    public static class ServiceCollectionExtensions
    {
        private static TimeSpan[] retryTimeSpans = new[] { TimeSpan.FromMilliseconds(500), TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5) };
        private static int numberOfExceptionsBeforeCircuitBreaker = 100;
        private static TimeSpan circuitBreakerFailurePeriod = TimeSpan.FromMinutes(1);

        public static IServiceCollection AddCosmosDb(this IServiceCollection builder, IConfiguration config, string collectionNameOverride = null)
        {
            CosmosDbSettings cosmosDbSettings = new CosmosDbSettings();

            config.Bind("CosmosDbSettings", cosmosDbSettings);

            if (!string.IsNullOrWhiteSpace(collectionNameOverride))
            {
                cosmosDbSettings.CollectionName = collectionNameOverride;
            }

            builder.AddSingleton<CosmosDbSettings>(cosmosDbSettings);

            builder
                .AddSingleton<CosmosRepository>();

            return builder;
        }

        public static IServiceCollection AddCalcsInterServiceClient(this IServiceCollection builder, IConfiguration config)
        {
            builder
                .AddSingleton<ICalcsApiClientProxy, CalcsApiProxy>((ctx) =>
                {
                    ApiOptions apiOptions = new ApiOptions();

                    config.Bind("calcsClient", apiOptions);

                    ILogger logger = ctx.GetService<ILogger>();
                    ICorrelationIdProvider correlationIdProvider = ctx.GetService<ICorrelationIdProvider>();

                    return new CalcsApiProxy(apiOptions, logger, correlationIdProvider);
                });

            return builder;
        }

        public static IServiceCollection AddDatasetsInterServiceClient(this IServiceCollection builder, IConfiguration config)
        {
            builder
                .AddSingleton<IDatasetsApiClientProxy, DatasetsApiProxy>((ctx) =>
                {
                    ApiOptions apiOptions = new ApiOptions();

                    config.Bind("datasetsClient", apiOptions);

                    ILogger logger = ctx.GetService<ILogger>();
                    ICorrelationIdProvider correlationIdProvider = ctx.GetService<ICorrelationIdProvider>();

                    return new DatasetsApiProxy(apiOptions, logger, correlationIdProvider);
                });

            return builder;
        }

        public static IServiceCollection AddScenariosInterServiceClient(this IServiceCollection builder, IConfiguration config)
        {
            builder
                 .AddSingleton<IScenariosApiClientProxy, ScenariosApiProxy>((ctx) =>
                 {
                     ApiOptions apiOptions = new ApiOptions();

                     config.Bind("scenariosClient", apiOptions);

                     ILogger logger = ctx.GetService<ILogger>();
                     ICorrelationIdProvider correlationIdProvider = ctx.GetService<ICorrelationIdProvider>();

                     return new ScenariosApiProxy(apiOptions, logger, correlationIdProvider);
                 });

            return builder;
        }

        public static IServiceCollection AddSpecificationsInterServiceClient(this IServiceCollection builder, IConfiguration config)
        {
            builder
                 .AddSingleton<ISpecificationsApiClientProxy, SpecificationsApiProxy>((ctx) =>
                 {
                     ApiOptions apiOptions = new ApiOptions();

                     config.Bind("specificationsClient", apiOptions);

                     ILogger logger = ctx.GetService<ILogger>();
                     ICorrelationIdProvider correlationIdProvider = ctx.GetService<ICorrelationIdProvider>();

                     return new SpecificationsApiProxy(apiOptions, logger, correlationIdProvider);
                 });

            return builder;
        }



        public static IServiceCollection AddResultsInterServiceClient(this IServiceCollection builder, IConfiguration config)
        {
            builder
                 .AddSingleton<IResultsApiClientProxy, ResultsApiProxy>((ctx) =>
                 {
                     ApiOptions apiOptions = new ApiOptions();

                     config.Bind("resultsClient", apiOptions);

                     ILogger logger = ctx.GetService<ILogger>();
                     ICorrelationIdProvider correlationIdProvider = ctx.GetService<ICorrelationIdProvider>();

                     return new ResultsApiProxy(apiOptions, logger, correlationIdProvider);
                 });

            return builder;
        }

        public static IServiceCollection AddJobsInterServiceClient(this IServiceCollection builder, IConfiguration config)
        {
            builder.AddHttpClient(HttpClientKeys.Jobs,
               c =>
               {
                   ApiOptions apiOptions = new ApiOptions();

                   config.Bind("jobsClient", apiOptions);

                   SetDefaultApiClientConfigurationOptions(c, apiOptions, builder);
               })
               .ConfigurePrimaryHttpMessageHandler(() => new ApiClientHandler())
               .AddTransientHttpErrorPolicy(c => c.WaitAndRetryAsync(retryTimeSpans))
               .AddTransientHttpErrorPolicy(c => c.CircuitBreakerAsync(numberOfExceptionsBeforeCircuitBreaker, circuitBreakerFailurePeriod));

            builder
                .AddSingleton<IJobsApiClient, JobsApiClient>();

            return builder;
        }

        public static IServiceCollection AddFeatureToggling(this IServiceCollection builder, IConfiguration config)
        {
            builder
                 .AddSingleton<IFeatureToggle, Features>((ctx) =>
                 {
                     IConfigurationSection featuresConfig = config.GetSection("features");
                     return new Features(featuresConfig);
                 });

            return builder;
        }

        public static IServiceCollection AddSearch(this IServiceCollection builder, IConfiguration config)
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

            builder
              .AddSingleton<ISearchRepository<DatasetDefinitionIndex>, SearchRepository<DatasetDefinitionIndex>>();

            builder
                .AddSingleton<ISearchRepository<AllocationNotificationFeedIndex>, SearchRepository<AllocationNotificationFeedIndex>>();

            builder
             .AddSingleton<ISearchRepository<ProviderCalculationResultsIndex>, SearchRepository<ProviderCalculationResultsIndex>>();

            builder
                .AddSingleton<ISearchRepository<DatasetVersionIndex>, SearchRepository<DatasetVersionIndex>>();

            return builder;
        }

        public static IServiceCollection AddServiceBus(this IServiceCollection builder, IConfiguration config)
        {
            if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
            {
                builder
                .AddSingleton<IMessengerService, QueueMessengerService>((ctx) =>
                {
                    return new QueueMessengerService("UseDevelopmentStorage=true");
                });
            }
            else
            {

                ServiceBusSettings serviceBusSettings = new ServiceBusSettings();

                config.Bind("ServiceBusSettings", serviceBusSettings);

                builder.AddSingleton(serviceBusSettings);

                builder
                    .AddSingleton<IMessengerService, MessengerService>();
            }

            return builder;
        }

        public static IServiceCollection AddLogging(this IServiceCollection builder, string serviceName, IConfigurationRoot config = null)
        {
            builder.AddSingleton<ICorrelationIdProvider, CorrelationIdProvider>();

            builder.AddSingleton<Serilog.ILogger>((ctx) =>
            {
                TelemetryClient client = ctx.GetService<TelemetryClient>();

                LoggerConfiguration loggerConfiguration = GetLoggerConfiguration(client, serviceName);

                if (config != null && !string.IsNullOrWhiteSpace(config.GetValue<string>("FileLoggingPath")))
                {
                    string folderPath = config.GetValue<string>("FileLoggingPath");

                    loggerConfiguration.WriteTo.RollingFile(folderPath + "log-{Date}-" + Environment.MachineName + ".txt", LogEventLevel.Verbose);
                }

                return loggerConfiguration.CreateLogger();
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

        public static IServiceCollection AddApplicationInsightsTelemetryClient(this IServiceCollection builder, IConfiguration config, string serviceName, TelemetryChannelType channelType = TelemetryChannelType.Default)
        {
            Guard.ArgumentNotNull(config, nameof(config));

            ApplicationInsightsOptions appInsightsOptions = new ApplicationInsightsOptions();

            config.Bind("ApplicationInsightsOptions", appInsightsOptions);

            string appInsightsKey = appInsightsOptions.InstrumentationKey;

            if (string.IsNullOrWhiteSpace(appInsightsKey))
            {
                throw new InvalidOperationException("Unable to lookup Application Insights Configuration key from Configuration Provider. The value returned was empty string");
            }

            TelemetryConfiguration appInsightsTelemetryConfiguration = TelemetryConfiguration.Active;
            appInsightsTelemetryConfiguration.InstrumentationKey = appInsightsKey;

            if (channelType == TelemetryChannelType.Sync)
            {
                appInsightsTelemetryConfiguration.TelemetryChannel = new SyncTelemetryChannel(appInsightsOptions.Url);
            }

            TelemetryClient telemetryClient = new TelemetryClient(appInsightsTelemetryConfiguration);

            telemetryClient.InstrumentationKey = appInsightsKey;

            if (!telemetryClient.Context.GlobalProperties.ContainsKey(LoggingConstants.ServiceNamePropertiesName))
            {
                telemetryClient.Context.GlobalProperties.Add(LoggingConstants.ServiceNamePropertiesName, serviceName);
            }

            builder.AddSingleton(telemetryClient);

            return builder;
        }

        public static IServiceScope CreateHttpScope(this IServiceProvider serviceProvider, HttpRequest request)
        {
            ICorrelationIdProvider correlationIdProvider = serviceProvider.GetService<ICorrelationIdProvider>();

            string correlationId = request.GetCorrelationId();

            correlationIdProvider.SetCorrelationId(correlationId);

            if (!request.HttpContext.Response.Headers.ContainsKey("sfa-correlationId"))
            {
                request.HttpContext.Response.Headers.Add("sfa-correlationId", correlationId);
            }

            string userId = "unknown";
            string username = "unknown";

            if (request.HttpContext.Request.Headers.ContainsKey("sfa-userid"))
            {
                userId = request.HttpContext.Request.Headers["sfa-userid"];
            }

            if (request.HttpContext.Request.Headers.ContainsKey("sfa-username"))
            {
                username = request.HttpContext.Request.Headers["sfa-username"];
            }

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
            .Enrich.FromLogContext()
            .WriteTo.ApplicationInsights(telemetryClient, TelemetryConverter.Traces);
        }

        public static IServiceCollection AddCaching(this IServiceCollection builder, IConfiguration config)
        {
            RedisSettings redisSettings = new RedisSettings();

            config.Bind("redisSettings", redisSettings);

            builder.AddSingleton<RedisSettings>(redisSettings);

            builder
                .AddSingleton<ICacheProvider, StackExchangeRedisClientCacheProvider>();

            return builder;
        }

        public static IServiceCollection AddEngineSettings(this IServiceCollection builder, IConfiguration config)
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

        public static IServiceCollection AddPolicySettings(this IServiceCollection builder, IConfiguration config)
        {
            PolicySettings policySettings = GetPolicySettings(builder, config);

            builder.AddSingleton<PolicySettings>(policySettings);

            return builder;
        }

        public static PolicySettings GetPolicySettings(this IServiceCollection builder, IConfiguration config)
        {
            PolicySettings policySettings = new PolicySettings();

            config.Bind("policy", policySettings);

            return policySettings;
        }

        public static IFeatureToggle CreateFeatureToggles(this IServiceCollection builder, IConfiguration config)
        {
            IConfigurationSection featuresConfig = config.GetSection("features");
            return new Features(featuresConfig);
        }

        private static void SetDefaultApiClientConfigurationOptions(HttpClient httpClient, ApiOptions options, IServiceCollection services)
        {
            Guard.ArgumentNotNull(httpClient, nameof(httpClient));
            Guard.ArgumentNotNull(options, nameof(options));
            Guard.ArgumentNotNull(services, nameof(services));

            if (string.IsNullOrWhiteSpace(options.ApiEndpoint))
            {
                throw new InvalidOperationException("options EndPoint is null or empty string");
            }

            string baseAddress = options.ApiEndpoint;
            if (!baseAddress.EndsWith("/", StringComparison.CurrentCulture))
            {
                baseAddress = $"{baseAddress}/";
            }

            IServiceProvider serviceProvider = services.BuildServiceProvider();


            httpClient.BaseAddress = new Uri(baseAddress, UriKind.Absolute);
            httpClient.DefaultRequestHeaders?.Add(ApiClientHeaders.ApiKey, options.ApiKey);

            httpClient.DefaultRequestHeaders?.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            httpClient.DefaultRequestHeaders?.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
            httpClient.DefaultRequestHeaders?.AcceptEncoding.Add(new StringWithQualityHeaderValue("deflate"));
        }
    }
}
