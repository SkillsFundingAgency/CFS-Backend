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
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Storage;
using CalculateFunding.Models.MappingProfiles;
using CalculateFunding.Models.Results;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Core.AspNet;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Services.Core.Interfaces.Services;
using CalculateFunding.Services.Core.Options;
using CalculateFunding.Services.Core.Services;
using CalculateFunding.Services.Providers;
using CalculateFunding.Services.Providers.Interfaces;
using CalculateFunding.Services.Results;
using CalculateFunding.Services.Results.Interfaces;
using CalculateFunding.Services.Results.Repositories;
using CalculateFunding.Services.Results.Validators;
using FluentValidation;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Bulkhead;
using Serilog;

namespace CalculateFunding.Functions.Results
{
    static public class IocConfig
    {
        private static IServiceProvider _serviceProvider;

        private static TimeSpan[] retryTimeSpans = new[] { TimeSpan.FromMilliseconds(500), TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5) };
        private static int numberOfExceptionsBeforeCircuitBreaker = 100;
        private static TimeSpan circuitBreakerFailurePeriod = TimeSpan.FromMinutes(1);


        public static IServiceProvider Build(IConfigurationRoot config)
        {
            if (_serviceProvider == null)
            {
                _serviceProvider = BuildServiceProvider(config);
            }

            return _serviceProvider;
        }

        public static IServiceProvider BuildServiceProvider(IConfigurationRoot config)
        {
            ServiceCollection serviceProvider = new ServiceCollection();

            RegisterComponents(serviceProvider, config);

            return serviceProvider.BuildServiceProvider();
        }

        public static IServiceProvider Build(Message message, IConfigurationRoot config)
        {
            if (_serviceProvider == null)
            {
                _serviceProvider = BuildServiceProvider(message, config);
            }

            IUserProfileProvider userProfileProvider = _serviceProvider.GetService<IUserProfileProvider>();

            Reference user = message.GetUserDetails();

            userProfileProvider.SetUser(user.Id, user.Name);

            return _serviceProvider;
        }

        public static IServiceProvider BuildServiceProvider(Message message, IConfigurationRoot config)
        {
            ServiceCollection serviceProvider = new ServiceCollection();

            serviceProvider.AddUserProviderFromMessage(message);

            RegisterComponents(serviceProvider, config);

            return serviceProvider.BuildServiceProvider();
        }

        public static void RegisterComponents(IServiceCollection builder, IConfigurationRoot config)
        {
            builder.AddSingleton<ICalculationResultsRepository, CalculationResultsRepository>();
            builder.AddSingleton<IResultsService, ResultsService>();
            builder.AddSingleton<IPublishedResultsService, PublishedResultsService>();
            builder.AddSingleton<IResultsSearchService, ResultsSearchService>();
            builder.AddSingleton<ICalculationProviderResultsSearchService, CalculationProviderResultsSearchService>();
            builder.AddSingleton<IProviderImportMappingService, ProviderImportMappingService>();
            builder.AddSingleton<IAllocationNotificationsFeedsSearchService, AllocationNotificationsFeedsSearchService>();
            builder.AddSingleton<ICalculationsRepository, CalculationsRepository>();
            builder.AddSingleton<IValidator<MasterProviderModel>, MasterProviderModelValidator>();
            builder.AddSingleton<IProviderVariationAssemblerService, ProviderVariationAssemblerService>();
            builder.AddSingleton<IProviderVariationsService, ProviderVariationsService>();
            builder.AddSingleton<IProviderService, ProviderService>();
            builder.AddSingleton<IJobHelperService, JobHelperService>();

            builder.AddSingleton<IProviderVariationsStorageRepository, ProviderVariationsStorageRepository>((ctx) =>
            {
                BlobStorageOptions blobStorageOptions = new BlobStorageOptions();

                config.Bind("CommonStorageSettings", blobStorageOptions);

                blobStorageOptions.ContainerName = "providervariations";

                return new ProviderVariationsStorageRepository(blobStorageOptions);
            });

            MapperConfiguration resultsConfig = new MapperConfiguration(c =>
            {
                c.AddProfile<DatasetsMappingProfile>();
                c.AddProfile<ResultServiceMappingProfile>();
                c.AddProfile<ProviderMappingProfile>();
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

            builder.AddSingleton<IPublishedProviderResultsRepository, PublishedProviderResultsRepository>((ctx) =>
            {
                CosmosDbSettings resultsDbSettings = new CosmosDbSettings();

                config.Bind("CosmosDbSettings", resultsDbSettings);

                resultsDbSettings.CollectionName = "publishedproviderresults";

                CosmosRepository resultsRepostory = new CosmosRepository(resultsDbSettings);

                return new PublishedProviderResultsRepository(resultsRepostory);
            });

            builder.AddSingleton<IProviderChangesRepository, ProviderChangesRepository>((ctx) =>
            {
                CosmosDbSettings cosmosSettings = new CosmosDbSettings();

                config.Bind("CosmosDbSettings", cosmosSettings);

                cosmosSettings.CollectionName = "publishedproviderchanges";

                CosmosRepository resultsRepostory = new CosmosRepository(cosmosSettings);

                ILogger logger = ctx.GetService<ILogger>();

                return new ProviderChangesRepository(resultsRepostory, logger);
            });

            builder
                .AddSingleton<ISpecificationsRepository, SpecificationsRepository>();

            builder
               .AddSingleton<IPublishedProviderResultsAssemblerService, PublishedProviderResultsAssemblerService>();

            builder.AddSingleton<IPublishedProviderResultsSettings, PublishedProviderResultsSettings>((ctx) =>
            {
                PublishedProviderResultsSettings settings = new PublishedProviderResultsSettings();

                config.Bind("PublishedProviderResultsSettings", settings);

                return settings;
            });

            builder.AddSingleton<IVersionRepository<PublishedAllocationLineResultVersion>, VersionRepository<PublishedAllocationLineResultVersion>>((ctx) =>
            {
                CosmosDbSettings versioningDbSettings = new CosmosDbSettings();

                config.Bind("CosmosDbSettings", versioningDbSettings);

                versioningDbSettings.CollectionName = "publishedproviderresults";

                CosmosRepository resultsRepostory = new CosmosRepository(versioningDbSettings);

                return new VersionRepository<PublishedAllocationLineResultVersion>(resultsRepostory);
            });

            builder.AddSearch(config);

            builder.AddServiceBus(config);

            builder.AddCaching(config);

            builder.AddApplicationInsights(config, "CalculateFunding.Functions.Results");
            builder.AddApplicationInsightsTelemetryClient(config, "CalculateFunding.Functions.Results");
            builder.AddLogging("CalculateFunding.Functions.Results");
            builder.AddTelemetry();

            builder.AddCalcsInterServiceClient(config);
            builder.AddSpecificationsInterServiceClient(config);
            builder.AddJobsInterServiceClient(config);
            builder.AddResultsInterServiceClient(config);

            builder.AddFeatureToggling(config);

            builder.AddSingleton<IPublishedAllocationLineLogicalResultVersionService>((ctx) =>
            {
                IFeatureToggle featureToggle = ctx.GetService<IFeatureToggle>();

                bool enableMajorMinorVersioning = featureToggle.IsAllocationLineMajorMinorVersioningEnabled();

                if (enableMajorMinorVersioning)
                {
                    return new PublishedAllocationLineLogicalResultVersionService();
                }
                else
                {
                    return new RedundantPublishedAllocationLineLogicalResultVersionService();
                }
            });

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

            builder.AddSingleton<IResultsResilliencePolicies>(resultsResiliencePolicies);
            builder.AddSingleton<IJobHelperResiliencePolicies>(resultsResiliencePolicies);
        }

        private static ResiliencePolicies CreateResiliencePolicies(PolicySettings policySettings)
        {
            BulkheadPolicy totalNetworkRequestsPolicy = ResiliencePolicyHelpers.GenerateTotalNetworkRequestsPolicy(policySettings);

            ResiliencePolicies resiliencePolicies = new ResiliencePolicies()
            {
                CalculationProviderResultsSearchRepository = SearchResiliencePolicyHelper.GenerateSearchPolicy(totalNetworkRequestsPolicy),
                ResultsRepository = CosmosResiliencePolicyHelper.GenerateCosmosPolicy(totalNetworkRequestsPolicy),
                ResultsSearchRepository = SearchResiliencePolicyHelper.GenerateSearchPolicy(totalNetworkRequestsPolicy),
                SpecificationsRepository = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                AllocationNotificationFeedSearchRepository = SearchResiliencePolicyHelper.GenerateSearchPolicy(totalNetworkRequestsPolicy),
                ProviderProfilingRepository = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                PublishedProviderCalculationResultsRepository = CosmosResiliencePolicyHelper.GenerateCosmosPolicy(totalNetworkRequestsPolicy),
                PublishedProviderResultsRepository = CosmosResiliencePolicyHelper.GenerateCosmosPolicy(totalNetworkRequestsPolicy),
                CalculationsRepository = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                JobsApiClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                ProviderCalculationResultsSearchRepository = SearchResiliencePolicyHelper.GenerateSearchPolicy(totalNetworkRequestsPolicy),
                ProviderChangesRepository = CosmosResiliencePolicyHelper.GenerateCosmosPolicy(totalNetworkRequestsPolicy),
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
