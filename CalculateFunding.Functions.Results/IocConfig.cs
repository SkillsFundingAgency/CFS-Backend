using System;
using AutoMapper;
using CalculateFunding.Common.FeatureToggles;
using CalculateFunding.Models;
using CalculateFunding.Models.MappingProfiles;
using CalculateFunding.Models.Results;
using CalculateFunding.Repositories.Common.Cosmos;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Services.Core.Interfaces.Proxies.External;
using CalculateFunding.Services.Core.Interfaces.Services;
using CalculateFunding.Services.Core.Options;
using CalculateFunding.Services.Core.Services;
using CalculateFunding.Services.Results;
using CalculateFunding.Services.Results.Interfaces;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly.Bulkhead;

namespace CalculateFunding.Functions.Results
{
    static public class IocConfig
    {
        private static IServiceProvider _serviceProvider;

        public static IServiceProvider Build(IConfigurationRoot config)
        {
            if (_serviceProvider == null)
            {
                _serviceProvider = BuildServiceProvider(config);
            }

            return _serviceProvider;
        }

        static public IServiceProvider BuildServiceProvider(IConfigurationRoot config)
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

        static public IServiceProvider BuildServiceProvider(Message message, IConfigurationRoot config)
        {
            ServiceCollection serviceProvider = new ServiceCollection();

            serviceProvider.AddUserProviderFromMessage(message);

            RegisterComponents(serviceProvider, config);

            return serviceProvider.BuildServiceProvider();
        }

        static public void RegisterComponents(IServiceCollection builder, IConfigurationRoot config)
        {
            builder.AddSingleton<ICalculationResultsRepository, CalculationResultsRepository>();
            builder.AddSingleton<IResultsService, ResultsService>();
            builder.AddSingleton<IPublishedResultsService, PublishedResultsService>();
            builder.AddSingleton<IResultsSearchService, ResultsSearchService>();
            builder.AddSingleton<ICalculationProviderResultsSearchService, CalculationProviderResultsSearchService>();
            builder.AddSingleton<IProviderImportMappingService, ProviderImportMappingService>();
            builder.AddSingleton<IAllocationNotificationsFeedsSearchService, AllocationNotificationsFeedsSearchService>();
            builder.AddSingleton<ICalculationsRepository, CalculationsRepository>();

            MapperConfiguration resultsConfig = new MapperConfiguration(c => c.AddProfile<DatasetsMappingProfile>());
            builder
                .AddSingleton(resultsConfig.CreateMapper());

            builder.AddSpecificationsInterServiceClient(config);

            builder.AddCalcsInterServiceClient(config);

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

            builder.AddSingleton<IPublishedProviderCalculationResultsRepository, PublishedProviderCalculationResultsRepository>((ctx) =>
            {
                CosmosDbSettings resultsDbSettings = new CosmosDbSettings();

                config.Bind("CosmosDbSettings", resultsDbSettings);

                resultsDbSettings.CollectionName = "publishedprovidercalcresults";

                CosmosRepository resultsRepostory = new CosmosRepository(resultsDbSettings);

                return new PublishedProviderCalculationResultsRepository(resultsRepostory);
            });

            builder
                .AddSingleton<ISpecificationsRepository, SpecificationsRepository>();

            builder
               .AddSingleton<IPublishedProviderResultsAssemblerService, PublishedProviderResultsAssemblerService>();

            builder.AddSingleton<IVersionRepository<PublishedAllocationLineResultVersion>, VersionRepository<PublishedAllocationLineResultVersion>>((ctx) =>
            {
                CosmosDbSettings versioningDbSettings = new CosmosDbSettings();

                config.Bind("CosmosDbSettings", versioningDbSettings);

                versioningDbSettings.CollectionName = "publishedproviderresults";

                CosmosRepository resultsRepostory = new CosmosRepository(versioningDbSettings);

                return new VersionRepository<PublishedAllocationLineResultVersion>(resultsRepostory);
            });

            builder.AddSingleton<IVersionRepository<PublishedProviderCalculationResultVersion>, VersionRepository<PublishedProviderCalculationResultVersion>>((ctx) =>
            {
                CosmosDbSettings versioningDbSettings = new CosmosDbSettings();

                config.Bind("CosmosDbSettings", versioningDbSettings);

                versioningDbSettings.CollectionName = "publishedprovidercalcresults";

                CosmosRepository resultsRepostory = new CosmosRepository(versioningDbSettings);

                return new VersionRepository<PublishedProviderCalculationResultVersion>(resultsRepostory);
            });

            builder.AddSearch(config);

            builder.AddServiceBus(config);

            builder.AddCaching(config);

            builder.AddApplicationInsightsTelemetryClient(config, "CalculateFunding.Functions.Results");
            builder.AddLogging("CalculateFunding.Functions.Results");
            builder.AddTelemetry();

            builder.AddSpecificationsInterServiceClient(config);

            builder.AddPolicySettings(config);

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

            builder.AddSingleton<IProviderProfilingRepository>((ctx) =>
            {
                IFeatureToggle featureToggle = ctx.GetService<IFeatureToggle>();

                bool enableMockProvider = featureToggle.IsProviderProfilingServiceDisabled();

                if (enableMockProvider)
                {
                    return new MockProviderProfilingRepository();
                }
                else
                {
                    IProviderProfilingApiProxy providerProfilingApiProxy = ctx.GetService<IProviderProfilingApiProxy>();

                    return new ProviderProfilingRepository(providerProfilingApiProxy);
                }
            });

            builder.AddSingleton<IResultsResilliencePolicies>((ctx) =>
            {
                PolicySettings policySettings = ctx.GetService<PolicySettings>();

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
                    CalculationsRepository = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy)
                };

                return resiliencePolicies;
            });
        }
    }
}
