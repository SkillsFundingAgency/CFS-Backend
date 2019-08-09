using System;
using System.Net.Http;
using CalculateFunding.Common.ApiClient;
using CalculateFunding.Common.ApiClient.Bearer;
using CalculateFunding.Common.ApiClient.Profiling;
using CalculateFunding.Common.Caching;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.Interfaces;
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
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.IoC;
using CalculateFunding.Services.Publishing.Providers;
using CalculateFunding.Services.Publishing.Repositories;
using CalculateFunding.Services.Publishing.Specifications;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
            builder.AddSingleton<IPublishedFundingRepository, PublishedFundingRepository>((ctx) =>
            {
                CosmosDbSettings calssDbSettings = new CosmosDbSettings();

                config.Bind("CosmosDbSettings", calssDbSettings);

                calssDbSettings.CollectionName = "publishedfunding";

                CosmosRepository calcsCosmosRepostory = new CosmosRepository(calssDbSettings);

                return new PublishedFundingRepository(calcsCosmosRepostory);
            });

            builder.AddSingleton<ICosmosRepository, CosmosRepository>();
            builder.AddCaching(config);
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
            builder.AddSingleton<IPublishService, PublishService>();
            builder.AddSingleton<ISpecificationFundingStatusService, SpecificationFundingStatusService>();

            builder
                .AddSingleton<IPublishedProviderVersioningService, PublishedProviderVersioningService>()
                .AddSingleton<IHealthChecker, PublishedProviderVersioningService>();

            builder
                .AddSingleton<IPublishedProviderStatusUpdateService, PublishedProviderStatusUpdateService>()
                .AddSingleton<IHealthChecker, PublishedProviderStatusUpdateService>();

            builder.AddSingleton<IPublishedProviderVersionService, PublishedProviderVersionService>()
                .AddSingleton<IHealthChecker, PublishedProviderStatusUpdateService>();

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

                publishedProviderVersioningDbSettings.CollectionName = "publishedfunding";

                CosmosRepository resultsRepostory = new CosmosRepository(publishedProviderVersioningDbSettings);

                return new VersionRepository<PublishedProviderVersion>(resultsRepostory);
            }).AddSingleton<IHealthChecker, VersionRepository<PublishedProviderVersion>>();


            builder.AddSingleton<ICalculationResultsRepository, CalculationResultsRepository>((ctx) =>
            {
                CosmosDbSettings calssDbSettings = new CosmosDbSettings();

                config.Bind("CosmosDbSettings", calssDbSettings);

                calssDbSettings.CollectionName = "calculationresults";

                CosmosRepository calcsCosmosRepostory = new CosmosRepository(calssDbSettings);

                return new CalculationResultsRepository(calcsCosmosRepostory);
            });

            builder.AddSingleton<IPublishedResultService, PublishedResultService>();

            builder.AddSingleton<IFundingLineGenerator, FundingLineGenerator>();

            builder.AddSingleton<IFundingLineTotalAggregator, FundingLineTotalAggregator>();

            builder.AddSingleton<IProfilingService, ProfilingService>();

            builder.AddSingleton<IInScopePublishedProviderService, InScopePublishedProviderService>();

            builder.AddSingleton<IPublishedProviderDataPopulator, PublishedProviderDataPopulator>();

            builder.AddSingleton<IPublishedProviderContentsGeneratorResolver>(r =>
            {
                PublishedProviderContentsGeneratorResolver resolver = new PublishedProviderContentsGeneratorResolver();

                IPublishedProviderContentsGenerator v10Generator = new Generators.Schema10.PublishedProviderContentsGenerator();

                resolver.Register("1.0", v10Generator);

                return resolver;
            });

            builder.AddSingleton<IJobHelperService, JobHelperService>();

            builder.AddApplicationInsights(config, "CalculateFunding.Functions.Publishing");
            builder.AddApplicationInsightsTelemetryClient(config, "CalculateFunding.Functions.Publishing");

            builder.AddLogging("CalculateFunding.Functions.Publishing");

            builder.AddTelemetry();

            PolicySettings policySettings = builder.GetPolicySettings(config);
            ResiliencePolicies publishingResiliencePolicies = CreateResiliencePolicies(policySettings);

            builder.AddPublishingServices(config);

            builder.AddSingleton<IPublishingResiliencePolicies>(publishingResiliencePolicies);

            builder.AddSpecificationsInterServiceClient(config);
            builder.AddProvidersInterServiceClient(config);
            builder.AddJobsInterServiceClient(config);

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

            return builder.BuildServiceProvider();
        }

        private static ResiliencePolicies CreateResiliencePolicies(PolicySettings policySettings)
        {
            BulkheadPolicy totalNetworkRequestsPolicy = ResiliencePolicyHelpers.GenerateTotalNetworkRequestsPolicy(policySettings);

            ResiliencePolicies resiliencePolicies = new ResiliencePolicies
            {
                ResultsRepository = CosmosResiliencePolicyHelper.GenerateCosmosPolicy(totalNetworkRequestsPolicy),
                JobsApiClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                ProvidersApiClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                PublishedProviderVersionRepository = CosmosResiliencePolicyHelper.GenerateCosmosPolicy(totalNetworkRequestsPolicy),
                SpecificationsRepositoryPolicy = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                BlobClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy)
            };

            return resiliencePolicies;
        }
    }
}