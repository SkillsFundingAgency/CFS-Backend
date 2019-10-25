using System;
using System.Net.Http;
using System.Net.Http.Headers;
using AutoMapper;
using CalculateFunding.Common.ApiClient;
using CalculateFunding.Common.ApiClient.Bearer;
using CalculateFunding.Common.ApiClient.Profiling;
using CalculateFunding.Common.Caching;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.FeatureToggles;
using CalculateFunding.Common.Interfaces;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.Storage;
using CalculateFunding.Common.Utility;
using CalculateFunding.Functions.Results.ServiceBus;
using CalculateFunding.Functions.Results.Timer;
using CalculateFunding.Models.MappingProfiles;
using CalculateFunding.Models.Results;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Core.AspNet;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Services.Core.Interfaces.AzureStorage;
using CalculateFunding.Services.Core.Interfaces.Services;
using CalculateFunding.Services.Core.Options;
using CalculateFunding.Services.Core.Services;
using CalculateFunding.Services.Results;
using CalculateFunding.Services.Results.Interfaces;
using CalculateFunding.Services.Results.Repositories;
using FluentValidation;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Bulkhead;
using Serilog;
using AzureStorage = CalculateFunding.Services.Core.AzureStorage;

[assembly: FunctionsStartup(typeof(CalculateFunding.Functions.Results.Startup))]

namespace CalculateFunding.Functions.Results
{
    public class Startup : FunctionsStartup
    {
        private static TimeSpan[] retryTimeSpans = new[] { TimeSpan.FromMilliseconds(500), TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5) };
        private static int numberOfExceptionsBeforeCircuitBreaker = 100;
        private static TimeSpan circuitBreakerFailurePeriod = TimeSpan.FromMinutes(1);

        public override void Configure(IFunctionsHostBuilder builder)
        {
            RegisterComponents(builder.Services);
        }

        public static IServiceProvider RegisterComponents(IServiceCollection builder)
        {
            IConfigurationRoot config = ConfigHelper.AddConfig();

            return RegisterComponents(builder, config);
        }

        public static IServiceProvider RegisterComponents(IServiceCollection builder, IConfigurationRoot config)
        {
            return Register(builder, config);
        }

        private static IServiceProvider Register(IServiceCollection builder, IConfigurationRoot config)
        {
            builder.AddSingleton<OnProviderResultsSpecificationCleanup>();
            builder.AddSingleton<OnReIndexCalculationResults>();
            builder.AddSingleton<OnProviderResultsSpecificationCleanup>();
            builder.AddSingleton<OnReIndexCalculationResults>();
            builder.AddSingleton<OnCalculationResultsCsvGeneration>();
            builder.AddSingleton<OnCalculationResultsCsvGenerationTimer>();
            builder.AddSingleton<ICalculationResultsRepository, CalculationResultsRepository>();
            builder.AddSingleton<IResultsService, ResultsService>();
            builder.AddSingleton<IJobManagement, JobManagement>();
            builder.AddSingleton<ICalculationProviderResultsSearchService, CalculationProviderResultsSearchService>();
            builder.AddSingleton<ICalculationsRepository, CalculationsRepository>();
            builder.AddSingleton<IJobHelperService, JobHelperService>();
            builder.AddSingleton<IProviderCalculationResultsReIndexerService, ProviderCalculationResultsReIndexerService>();

            MapperConfiguration resultsConfig = new MapperConfiguration(c =>
            {
                c.AddProfile<DatasetsMappingProfile>();
            });

            builder
                .AddSingleton(resultsConfig.CreateMapper());

            builder.AddCaching(config);

            builder.AddSingleton<ICalculationResultsRepository, CalculationResultsRepository>((ctx) =>
            {
                CosmosDbSettings calssDbSettings = new CosmosDbSettings();

                config.Bind("CosmosDbSettings", calssDbSettings);

                calssDbSettings.CollectionName = "calculationresults";

                CosmosRepository calcsCosmosRepostory = new CosmosRepository(calssDbSettings);

                return new CalculationResultsRepository(calcsCosmosRepostory);
            });

            builder.AddSingleton<IProviderSourceDatasetRepository, ProviderSourceDatasetRepository>((ctx) =>
            {
                CosmosDbSettings provDbSettings = new CosmosDbSettings();

                config.Bind("CosmosDbSettings", provDbSettings);

                provDbSettings.CollectionName = "providerdatasets";

                CosmosRepository calcsCosmosRepostory = new CosmosRepository(provDbSettings);

                return new ProviderSourceDatasetRepository(calcsCosmosRepostory);
            });

            builder
                .AddSingleton<IBlobClient, AzureStorage.BlobClient>((ctx) =>
                {
                    AzureStorageSettings storageSettings = new AzureStorageSettings();

                    config.Bind("AzureStorageSettings", storageSettings);

                    storageSettings.ContainerName = "datasets";

                    return new AzureStorage.BlobClient(storageSettings);
                });

            builder.AddSearch(config);

            builder.AddServiceBus(config);

            builder.AddCaching(config);

            builder.AddApplicationInsightsForFunctionApps(config, "CalculateFunding.Functions.Results");
            builder.AddApplicationInsightsTelemetryClient(config, "CalculateFunding.Functions.Results");
            builder.AddLogging("CalculateFunding.Functions.Results");
            builder.AddTelemetry();

            builder.AddCalculationsInterServiceClient(config);
            builder.AddSpecificationsInterServiceClient(config);
            builder.AddJobsInterServiceClient(config);
            builder.AddResultsInterServiceClient(config);
            builder.AddProvidersInterServiceClient(config);
            builder.AddPoliciesInterServiceClient(config);

            builder.AddFeatureToggling(config);

            builder.AddSingleton<ICancellationTokenProvider, InactiveCancellationTokenProvider>();

            builder.AddSingleton<IAzureBearerTokenProxy, AzureBearerTokenProxy>();

            builder.AddHttpClient(HttpClientKeys.Profiling,
                c =>
                {
                    ApiClientConfigurationOptions opts = new ApiClientConfigurationOptions();
                    config.Bind("providerProfilingClient", opts);

                    SetDefaultApiClientConfigurationOptions(c, opts, builder);
                })
                .ConfigurePrimaryHttpMessageHandler(() => new ApiClientHandler())
                .AddTransientHttpErrorPolicy(c => c.WaitAndRetryAsync(retryTimeSpans))
                .AddTransientHttpErrorPolicy(c => c.CircuitBreakerAsync(numberOfExceptionsBeforeCircuitBreaker, circuitBreakerFailurePeriod));

            builder.AddSingleton<IProfilingApiClient>((ctx) =>
            {
                IFeatureToggle featureToggle = ctx.GetService<IFeatureToggle>();

                bool enableMockProvider = featureToggle.IsProviderProfilingServiceDisabled();

                if (enableMockProvider)
                {
                    return new MockProviderProfilingRepository();
                }
                else
                {
                    IHttpClientFactory httpClientFactory = ctx.GetService<IHttpClientFactory>();
                    ILogger logger = ctx.GetService<ILogger>();
                    ICancellationTokenProvider cancellationTokenProvider = ctx.GetService<ICancellationTokenProvider>();

                    IAzureBearerTokenProxy azureBearerTokenProxy = ctx.GetService<IAzureBearerTokenProxy>();
                    ICacheProvider cacheProvider = ctx.GetService<ICacheProvider>();

                    AzureBearerTokenOptions azureBearerTokenOptions = new AzureBearerTokenOptions();
                    config.Bind("providerProfilingAzureBearerTokenOptions", azureBearerTokenOptions);

                    AzureBearerTokenProvider bearerTokenProvider = new AzureBearerTokenProvider(azureBearerTokenProxy, cacheProvider, azureBearerTokenOptions);

                    return new ProfilingApiClient(httpClientFactory, HttpClientKeys.Profiling, logger, bearerTokenProvider, cancellationTokenProvider);
                }
            });

            PolicySettings policySettings = builder.GetPolicySettings(config);
            ResiliencePolicies resultsResiliencePolicies = CreateResiliencePolicies(policySettings);

            builder.AddSingleton<IResultsResiliencePolicies>(resultsResiliencePolicies);
            builder.AddSingleton<IJobHelperResiliencePolicies>(resultsResiliencePolicies);
            
            builder.AddSingleton<IJobManagementResiliencePolicies>((ctx) =>
            {
                BulkheadPolicy totalNetworkRequestsPolicy = ResiliencePolicyHelpers.GenerateTotalNetworkRequestsPolicy(policySettings);

                return new JobManagementResiliencePolicies()
                {
                    JobsApiClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy)
                };
            });

            return builder.BuildServiceProvider();
        }

        private static ResiliencePolicies CreateResiliencePolicies(PolicySettings policySettings)
        {
            BulkheadPolicy totalNetworkRequestsPolicy = ResiliencePolicyHelpers.GenerateTotalNetworkRequestsPolicy(policySettings);

            ResiliencePolicies resiliencePolicies = new ResiliencePolicies()
            {
                CalculationProviderResultsSearchRepository = SearchResiliencePolicyHelper.GenerateSearchPolicy(totalNetworkRequestsPolicy),
                ResultsRepository = CosmosResiliencePolicyHelper.GenerateCosmosPolicy(totalNetworkRequestsPolicy),
                ResultsSearchRepository = SearchResiliencePolicyHelper.GenerateSearchPolicy(totalNetworkRequestsPolicy),
                SpecificationsApiClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                ProviderProfilingRepository = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                PublishedProviderCalculationResultsRepository = CosmosResiliencePolicyHelper.GenerateCosmosPolicy(totalNetworkRequestsPolicy),
                PublishedProviderResultsRepository = CosmosResiliencePolicyHelper.GenerateCosmosPolicy(totalNetworkRequestsPolicy),
                CalculationsRepository = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                JobsApiClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                ProviderCalculationResultsSearchRepository = SearchResiliencePolicyHelper.GenerateSearchPolicy(totalNetworkRequestsPolicy),
                ProviderChangesRepository = CosmosResiliencePolicyHelper.GenerateCosmosPolicy(totalNetworkRequestsPolicy),
                PoliciesApiClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy)
            };

            return resiliencePolicies;
        }

        private static void SetDefaultApiClientConfigurationOptions(HttpClient httpClient, ApiClientConfigurationOptions options, IServiceCollection services)
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
