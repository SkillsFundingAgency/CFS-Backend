﻿using System;
using System.Net.Http;
using AutoMapper;
using CalculateFunding.Common.ApiClient;
using CalculateFunding.Common.ApiClient.Bearer;
using CalculateFunding.Common.ApiClient.Policies;
using CalculateFunding.Common.ApiClient.Profiling;
using CalculateFunding.Common.Caching;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.Interfaces;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Functions.Publishing;
using CalculateFunding.Functions.Publishing.ServiceBus;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core.AspNet;
using CalculateFunding.Services.Core.AzureStorage;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Services.Core.Interfaces.AzureStorage;
using CalculateFunding.Services.Core.Interfaces.Services;
using CalculateFunding.Services.Core.Options;
using CalculateFunding.Services.Core.Services;
using CalculateFunding.Services.Publishing;
using CalculateFunding.Services.Publishing.Helper;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.IoC;
using CalculateFunding.Services.Publishing.Providers;
using CalculateFunding.Services.Publishing.Repositories;
using CalculateFunding.Services.Publishing.Specifications;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Polly;
using Polly.Bulkhead;
using Serilog;

[assembly: FunctionsStartup(typeof(Startup))]

namespace CalculateFunding.Functions.Publishing
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            RegisterComponents(builder.Services);
        }

        public static IServiceProvider RegisterComponents(IServiceCollection builder)
        {
            IConfigurationRoot config = ConfigHelper.AddConfig();

            return RegisterComponents(builder, config);
        }

        public static IServiceProvider RegisterComponents(IServiceCollection builder,
            IConfigurationRoot config)
        {
            return Register(builder, config);
        }

        private static IServiceProvider Register(IServiceCollection builder,
            IConfigurationRoot config)
        {
            builder.AddSingleton<IConfiguration>(ctx => config);

            builder.AddSingleton<IPublishedFundingRepository, PublishedFundingRepository>((ctx) =>
            {
                CosmosDbSettings calssDbSettings = new CosmosDbSettings();

                config.Bind("CosmosDbSettings", calssDbSettings);

                calssDbSettings.ContainerName = "publishedfunding";

                CosmosRepository calcsCosmosRepostory = new CosmosRepository(calssDbSettings);

                return new PublishedFundingRepository(calcsCosmosRepostory);
            });

            builder.AddSingleton<ICosmosRepository, CosmosRepository>();
            builder.AddCaching(config);
            builder.AddSearch(config);
            builder.AddSingleton<OnRefreshFunding>();
            builder.AddSingleton<OnApproveFunding>();
            builder.AddSingleton<OnPublishFunding>();
            builder.AddSingleton<OnRefreshFundingFailure>();
            builder.AddSingleton<OnApproveFundingFailure>();
            builder.AddSingleton<OnPublishFundingFailure>();

            builder.AddSingleton<ISpecificationService, SpecificationService>();
            builder.AddSingleton<IProviderService, ProviderService>();
            builder.AddSingleton<IRefreshService, RefreshService>();
            builder.AddSingleton<IApproveService, ApproveService>();
            builder.AddSingleton<IJobTracker, JobTracker>();
            builder.AddSingleton<IPublishService, PublishService>();
            builder.AddSingleton<IJobManagement, JobManagement>();
            builder.AddSingleton<ISpecificationFundingStatusService, SpecificationFundingStatusService>();

            builder
                .AddSingleton<IPublishedProviderVersioningService, PublishedProviderVersioningService>()
                .AddSingleton<IHealthChecker, PublishedProviderVersioningService>();

            builder
                .AddSingleton<IPublishedProviderStatusUpdateService, PublishedProviderStatusUpdateService>()
                .AddSingleton<IHealthChecker, PublishedProviderStatusUpdateService>();

            builder.AddSingleton<IPublishedProviderVersionService, PublishedProviderVersionService>()
                .AddSingleton<IHealthChecker, PublishedProviderStatusUpdateService>();

            builder.AddSingleton<IPublishedSearchService, PublishedSearchService>()
                    .AddSingleton<IHealthChecker, PublishedSearchService>();
            builder
                .AddSingleton<IPublishedProviderStatusUpdateSettings>(_ =>
                    {
                        PublishedProviderStatusUpdateSettings settings = new PublishedProviderStatusUpdateSettings();

                        config.Bind("PublishedProviderStatusUpdateSettings", settings);

                        return settings;
                    }
                );

            builder
                .AddSingleton<IPublishingEngineOptions>(_ => new PublishingEngineOptions(config));

            builder
               .AddSingleton<IBlobClient, BlobClient>((ctx) =>
               {
                   AzureStorageSettings storageSettings = new AzureStorageSettings();

                   config.Bind("AzureStorageSettings", storageSettings);

                   storageSettings.ContainerName = "publishedproviderversions";

                   return new BlobClient(storageSettings);
               });

            builder.AddSingleton<IVersionRepository<PublishedProviderVersion>, VersionRepository<PublishedProviderVersion>>((ctx) =>
            {
                CosmosDbSettings publishedProviderVersioningDbSettings = new CosmosDbSettings();

                config.Bind("CosmosDbSettings", publishedProviderVersioningDbSettings);

                publishedProviderVersioningDbSettings.ContainerName = "publishedfunding";

                CosmosRepository resultsRepostory = new CosmosRepository(publishedProviderVersioningDbSettings);

                return new VersionRepository<PublishedProviderVersion>(resultsRepostory);
            }).AddSingleton<IHealthChecker, VersionRepository<PublishedProviderVersion>>();

            builder.AddSingleton<IVersionRepository<PublishedFundingVersion>, VersionRepository<PublishedFundingVersion>>((ctx) =>
            {
                CosmosDbSettings ProviderSourceDatasetVersioningDbSettings = new CosmosDbSettings();

                config.Bind("CosmosDbSettings", ProviderSourceDatasetVersioningDbSettings);

                ProviderSourceDatasetVersioningDbSettings.ContainerName = "publishedfunding";

                CosmosRepository cosmosRepository = new CosmosRepository(ProviderSourceDatasetVersioningDbSettings);

                return new VersionRepository<PublishedFundingVersion>(cosmosRepository);
            });


            builder.AddSingleton<ICalculationResultsRepository, CalculationResultsRepository>((ctx) =>
            {
                CosmosDbSettings calssDbSettings = new CosmosDbSettings();

                config.Bind("CosmosDbSettings", calssDbSettings);

                calssDbSettings.ContainerName = "calculationresults";

                CosmosRepository calcsCosmosRepostory = new CosmosRepository(calssDbSettings);

                return new CalculationResultsRepository(calcsCosmosRepostory);
            });

            builder.AddSingleton<ICalculationResultsService, CalculationResultsService>();

            builder.AddSingleton<IPublishedProviderDataGenerator, PublishedProviderDataGenerator>();

            builder.AddSingleton<IFundingLineTotalAggregator, FundingLineTotalAggregator>();

            builder.AddSingleton<IProfilingService, ProfilingService>();

            builder.AddSingleton<IInScopePublishedProviderService, InScopePublishedProviderService>();

            builder.AddSingleton(new MapperConfiguration(_ =>
            {
                _.AddProfile<PublishingServiceMappingProfile>();
            }).CreateMapper());

            builder.AddSingleton<IPublishedProviderDataPopulator, PublishedProviderDataPopulator>();

            builder.AddSingleton<IRefreshPrerequisiteChecker, RefreshPrerequisiteChecker>();

            builder.AddSingleton<ICalculationEngineRunningChecker, CalculationEngineRunningChecker>();

            builder.AddSingleton<ICalculationPrerequisiteCheckerService, CalculationPrerequisiteCheckerService>();

            builder.AddSingleton<ICalculationsService, CalculationsService>();

            builder.AddSingleton<IPublishedProviderContentsGeneratorResolver>(ctx =>
            {
                PublishedProviderContentsGeneratorResolver resolver = new PublishedProviderContentsGeneratorResolver();

                IPublishedProviderContentsGenerator v10Generator = new Generators.Schema10.PublishedProviderContentsGenerator();

                resolver.Register("1.0", v10Generator);

                return resolver;
            });

            builder.AddSingleton<IPublishedFundingContentsGeneratorResolver>(ctx =>
            {
                PublishedFundingContentsGeneratorResolver resolver = new PublishedFundingContentsGeneratorResolver();

                IPublishedFundingContentsGenerator v10Generator = new Generators.Schema10.PublishedFundingContentsGenerator();

                resolver.Register("1.0", v10Generator);

                return resolver;
            });

            builder.AddSingleton<IPublishedFundingIdGeneratorResolver>(ctx =>
            {
                PublishedFundingIdGeneratorResolver resolver = new PublishedFundingIdGeneratorResolver();

                IPublishedFundingIdGenerator v10Generator = new Generators.Schema10.PublishedFundingIdGenerator();

                resolver.Register("1.0", v10Generator);

                return resolver;
            });

            builder.AddSingleton<IJobHelperService, JobHelperService>();

            builder.AddApplicationInsightsForFunctionApps(config, "CalculateFunding.Functions.Publishing");
            // builder.AddApplicationInsightsTelemetryClient(config, "CalculateFunding.Functions.Publishing");

            builder.AddLogging("CalculateFunding.Functions.Publishing", config);

            builder.AddTelemetry();

            PolicySettings policySettings = builder.GetPolicySettings(config);
            ResiliencePolicies publishingResiliencePolicies = CreateResiliencePolicies(policySettings);

            builder.AddSingleton<IJobManagementResiliencePolicies>((ctx) =>
            {
                BulkheadPolicy totalNetworkRequestsPolicy = ResiliencePolicyHelpers.GenerateTotalNetworkRequestsPolicy(policySettings);

                return new JobManagementResiliencePolicies()
                {
                    JobsApiClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy)
                };

            });

            builder.AddPublishingServices(config);

            builder.AddSingleton<IPublishingResiliencePolicies>(publishingResiliencePolicies);

            builder.AddSingleton<IJobHelperResiliencePolicies>(publishingResiliencePolicies);

            // Fix recommended by Microsoft for issues with disposed scopes when running in functions in the cloud
            builder.Configure<HttpClientFactoryOptions>(options => options.SuppressHandlerScope = true);

            builder.AddSpecificationsInterServiceClient(config);
            builder.AddProvidersInterServiceClient(config);
            builder.AddJobsInterServiceClient(config);
            builder.AddCalculationsInterServiceClient(config);

            builder.AddHttpClient(HttpClientKeys.Profiling,
                   c =>
                   {
                       ApiOptions apiOptions = new ApiOptions();

                       config.Bind("providerProfilingClient", apiOptions);

                       ServiceCollectionExtensions.SetDefaultApiClientConfigurationOptions(c, apiOptions, builder);
                   })
                   .ConfigurePrimaryHttpMessageHandler(() => new ApiClientHandler())
                   .AddTransientHttpErrorPolicy(c => c.WaitAndRetryAsync(new[] { TimeSpan.FromMilliseconds(500), TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5) }))
                   .AddTransientHttpErrorPolicy(c => c.CircuitBreakerAsync(100, TimeSpan.FromSeconds(30)));

            builder.AddSingleton<ICancellationTokenProvider, InactiveCancellationTokenProvider>();

            builder.AddSingleton<IAzureBearerTokenProxy, AzureBearerTokenProxy>();

            builder.AddSingleton<IProfilingApiClient>((ctx) =>
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
            });

            builder.AddHttpClient(HttpClientKeys.Policies,
                   c =>
                   {
                       ApiOptions apiOptions = new ApiOptions();

                       config.Bind("policiesClient", apiOptions);

                       ServiceCollectionExtensions.SetDefaultApiClientConfigurationOptions(c, apiOptions, builder);
                   })
               .ConfigurePrimaryHttpMessageHandler(() => new ApiClientHandler())
               .AddTransientHttpErrorPolicy(c => c.WaitAndRetryAsync(new[] { TimeSpan.FromMilliseconds(500), TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5) }))
               .AddTransientHttpErrorPolicy(c => c.CircuitBreakerAsync(100, TimeSpan.FromSeconds(30)));

            builder
                .AddSingleton<IPoliciesApiClient, PoliciesApiClient>();

            return builder.BuildServiceProvider();
        }

        private static ResiliencePolicies CreateResiliencePolicies(PolicySettings policySettings)
        {
            BulkheadPolicy totalNetworkRequestsPolicy = ResiliencePolicyHelpers.GenerateTotalNetworkRequestsPolicy(policySettings);

            ResiliencePolicies resiliencePolicies = new ResiliencePolicies
            {
                CalculationResultsRepository = CosmosResiliencePolicyHelper.GenerateCosmosPolicy(totalNetworkRequestsPolicy),
                JobsApiClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                ProvidersApiClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                PublishedProviderVersionRepository = CosmosResiliencePolicyHelper.GenerateCosmosPolicy(totalNetworkRequestsPolicy),
                SpecificationsRepositoryPolicy = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                BlobClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                CalculationsApiClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                PublishedFundingRepository = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                PoliciesApiClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                FundingFeedSearchRepository = Repositories.Common.Search.SearchResiliencePolicyHelper.GenerateSearchPolicy(totalNetworkRequestsPolicy),
                PublishedFundingBlobRepository = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                PublishedProviderSearchRepository = Repositories.Common.Search.SearchResiliencePolicyHelper.GenerateSearchPolicy(totalNetworkRequestsPolicy),
                PublishedIndexSearchResiliencePolicy = PublishedIndexSearchResiliencePolicy.GeneratePublishedIndexSearch(totalNetworkRequestsPolicy)
            };

            return resiliencePolicies;
        }
    }
}