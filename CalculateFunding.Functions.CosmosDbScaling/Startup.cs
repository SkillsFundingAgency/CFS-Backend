using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Functions.CosmosDbScaling.EventHubs;
using CalculateFunding.Functions.CosmosDbScaling.ServiceBus;
using CalculateFunding.Functions.CosmosDbScaling.Timer;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Options;
using CalculateFunding.Services.CosmosDbScaling;
using CalculateFunding.Services.CosmosDbScaling.Interfaces;
using CalculateFunding.Services.CosmosDbScaling.Repositories;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly.Bulkhead;
using System;

[assembly: FunctionsStartup(typeof(CalculateFunding.Functions.CosmosDbScaling.Startup))]

namespace CalculateFunding.Functions.CosmosDbScaling
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

        public static IServiceProvider RegisterComponents(IServiceCollection builder, IConfigurationRoot config)
        {
            return Register(builder, config);
        }

        private static IServiceProvider Register(IServiceCollection builder, IConfigurationRoot config)
        {
            builder.AddSingleton<OnCosmosDbDiagnosticsReceived>();

            builder.AddSingleton<OnScaleUpCosmosDbCollection>();

            builder.AddSingleton<OnIncrementalScaleDownCosmosDbCollection>();

            builder.AddSingleton<OnScaleDownCosmosDbCollection>();

            builder.AddSingleton<ICosmosDbScalingRepositoryProvider, CosmosDbScalingRepositoryProvider>();

            builder.AddSingleton<ICosmosDbScalingService, CosmosDbScalingService>();

            builder.AddSingleton<ICosmosDbScalingRequestModelBuilder, CosmosDbScalingRequestModelBuilder>();

            builder.AddSingleton<ICosmosDbThrottledEventsFilter, CosmosDbThrottledEventsFilter>();

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

            builder.AddSingleton<CalculationsScalingRepository>((ctx) =>
            {
                CosmosDbSettings cosmosDbSettings = new CosmosDbSettings();

                config.Bind("CosmosDbSettings", cosmosDbSettings);

                cosmosDbSettings.CollectionName = "calcs";

                CosmosRepository cosmosRepository = new CosmosRepository(cosmosDbSettings);

                return new CalculationsScalingRepository(cosmosRepository);
            });

            builder.AddSingleton<JobsScalingRepository>((ctx) =>
            {
                CosmosDbSettings cosmosDbSettings = new CosmosDbSettings();

                config.Bind("CosmosDbSettings", cosmosDbSettings);

                cosmosDbSettings.CollectionName = "jobs";

                CosmosRepository cosmosRepository = new CosmosRepository(cosmosDbSettings);

                return new JobsScalingRepository(cosmosRepository);
            });

            builder.AddSingleton<DatasetAggregationsScalingRepository>((ctx) =>
            {
                CosmosDbSettings cosmosDbSettings = new CosmosDbSettings();

                config.Bind("CosmosDbSettings", cosmosDbSettings);

                cosmosDbSettings.CollectionName = "datasetaggregations";

                CosmosRepository cosmosRepository = new CosmosRepository(cosmosDbSettings);

                return new DatasetAggregationsScalingRepository(cosmosRepository);
            });

            builder.AddSingleton<DatasetsScalingRepository>((ctx) =>
            {
                CosmosDbSettings cosmosDbSettings = new CosmosDbSettings();

                config.Bind("CosmosDbSettings", cosmosDbSettings);

                cosmosDbSettings.CollectionName = "datasets";

                CosmosRepository cosmosRepository = new CosmosRepository(cosmosDbSettings);

                return new DatasetsScalingRepository(cosmosRepository);
            });

            builder.AddSingleton<ProfilingScalingRepository>((ctx) =>
            {
                CosmosDbSettings cosmosDbSettings = new CosmosDbSettings();

                config.Bind("CosmosDbSettings", cosmosDbSettings);

                cosmosDbSettings.CollectionName = "profiling";

                CosmosRepository cosmosRepository = new CosmosRepository(cosmosDbSettings);

                return new ProfilingScalingRepository(cosmosRepository);
            });

            builder.AddSingleton<SpecificationsScalingRepository>((ctx) =>
            {
                CosmosDbSettings cosmosDbSettings = new CosmosDbSettings();

                config.Bind("CosmosDbSettings", cosmosDbSettings);

                cosmosDbSettings.CollectionName = "specs";

                CosmosRepository cosmosRepository = new CosmosRepository(cosmosDbSettings);

                return new SpecificationsScalingRepository(cosmosRepository);
            });

            builder.AddSingleton<TestResultsScalingRepository>((ctx) =>
            {
                CosmosDbSettings cosmosDbSettings = new CosmosDbSettings();

                config.Bind("CosmosDbSettings", cosmosDbSettings);

                cosmosDbSettings.CollectionName = "testresults";

                CosmosRepository cosmosRepository = new CosmosRepository(cosmosDbSettings);

                return new TestResultsScalingRepository(cosmosRepository);
            });

            builder.AddSingleton<TestsScalingRepository>((ctx) =>
            {
                CosmosDbSettings cosmosDbSettings = new CosmosDbSettings();

                config.Bind("CosmosDbSettings", cosmosDbSettings);

                cosmosDbSettings.CollectionName = "tests";

                CosmosRepository cosmosRepository = new CosmosRepository(cosmosDbSettings);

                return new TestsScalingRepository(cosmosRepository);
            });

            builder.AddSingleton<UsersScalingRepository>((ctx) =>
            {
                CosmosDbSettings cosmosDbSettings = new CosmosDbSettings();

                config.Bind("CosmosDbSettings", cosmosDbSettings);

                cosmosDbSettings.CollectionName = "users";

                CosmosRepository cosmosRepository = new CosmosRepository(cosmosDbSettings);

                return new UsersScalingRepository(cosmosRepository);
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

            return builder.BuildServiceProvider();
        }
    }
}
