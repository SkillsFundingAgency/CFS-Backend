using System;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Options;
using CalculateFunding.Services.CosmosDbScaling;
using CalculateFunding.Services.CosmosDbScaling.Interfaces;
using CalculateFunding.Services.CosmosDbScaling.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly.Bulkhead;

namespace CalculateFunding.Functions.CosmosDbScaling
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

        public static void RegisterComponents(IServiceCollection builder, IConfigurationRoot config)
        {
            builder.AddSingleton<ICosmosDbScalingRepositoryProvider, CosmosDbScalingRepositoryProvider>();

            builder.AddSingleton<ICosmosDbScalingService, CosmosDbScalingService>();

            builder.AddSingleton<ICosmosDbScalingRequestModelBuilder, CosmosDbScalingRequestModelBuilder>();

            builder.AddSingleton<CalculationProviderResultsScalingRepository>((ctx) =>
            {
                CosmosDbSettings cosmosDbSettings = new CosmosDbSettings();

                config.Bind("CosmosDbSettings", cosmosDbSettings);

                cosmosDbSettings.CollectionName = "calculationresults";

                CosmosRepository cosmosRepostory = new CosmosRepository(cosmosDbSettings);

                return new CalculationProviderResultsScalingRepository(cosmosRepostory);
            });
            
            builder.AddSingleton<ProviderSourceDatasetsScalingRepository>((ctx) =>
            {
                CosmosDbSettings cosmosDbSettings = new CosmosDbSettings();

                config.Bind("CosmosDbSettings", cosmosDbSettings);

                cosmosDbSettings.CollectionName = "providerdatasets";

                CosmosRepository cosmosRepository = new CosmosRepository(cosmosDbSettings);

                return new ProviderSourceDatasetsScalingRepository(cosmosRepository);
            });

            builder.AddSingleton<PublishedProviderResultsScalingRepository>((ctx) =>
            {
                CosmosDbSettings cosmosDbSettings = new CosmosDbSettings();

                config.Bind("CosmosDbSettings", cosmosDbSettings);

                cosmosDbSettings.CollectionName = "publishedproviderresults";

                CosmosRepository cosmosRepository = new CosmosRepository(cosmosDbSettings);

                return new PublishedProviderResultsScalingRepository(cosmosRepository);
            });

            builder.AddSingleton<ICosmosDbScalingConfigRepository>((ctx) =>
            {
                CosmosDbSettings cosmosDbSettings = new CosmosDbSettings();

                config.Bind("CosmosDbSettings", cosmosDbSettings);

                cosmosDbSettings.CollectionName = "cosmosscalingconfig";

                CosmosRepository cosmosRepository = new CosmosRepository(cosmosDbSettings);

                return new CosmosDbScalingConfigRepository(cosmosRepository);
            });

            builder.AddCaching(config);

            builder.AddJobsInterServiceClient(config);

            builder.AddServiceBus(config);

            builder.AddApplicationInsightsTelemetryClient(config, "CalculateFunding.Functions.CosmosDbScaling");

            builder.AddLogging("CalculateFunding.Functions.CosmosDbScaling", config);

            builder.AddTelemetry();

            builder.AddFeatureToggling(config);

            builder.AddSingleton<ICosmosDbScallingResilliencePolicies>(m =>
            {
                PolicySettings policySettings = builder.GetPolicySettings(config);

                BulkheadPolicy totalNetworkRequestsPolicy = ResiliencePolicyHelpers.GenerateTotalNetworkRequestsPolicy(policySettings);

                CosmosDbScallingResilliencePolicies resiliencePolicies = new CosmosDbScallingResilliencePolicies()
                {
                    ScalingRepository = CosmosResiliencePolicyHelper.GenerateCosmosPolicy(totalNetworkRequestsPolicy),
                    ScalingConfigRepository = CosmosResiliencePolicyHelper.GenerateCosmosPolicy(totalNetworkRequestsPolicy),
                    JobsApiClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                    CacheProvider = ResiliencePolicyHelpers.GenerateRedisPolicy(totalNetworkRequestsPolicy)
                };

                return resiliencePolicies;
            });
        }

    }
}
