using System;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Models.CosmosDbScaling;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Options;
using CalculateFunding.Services.CosmosDbScaling;
using CalculateFunding.Services.CosmosDbScaling.Interfaces;
using CalculateFunding.Services.CosmosDbScaling.Repositories;
using CalculateFunding.Services.CosmosDbScaling.Validators;
using FluentValidation;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly.Bulkhead;

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
            builder.AddSingleton<ICosmosDbScalingRepositoryProvider, CosmosDbScalingRepositoryProvider>();

            builder.AddSingleton<ICosmosDbScalingService, CosmosDbScalingService>();

            builder.AddSingleton<ICosmosDbScalingRequestModelBuilder, CosmosDbScalingRequestModelBuilder>();

            builder.AddSingleton<ICosmosDbThrottledEventsFilter, CosmosDbThrottledEventsFilter>();
            builder.AddSingleton<IValidator<ScalingConfigurationUpdateModel>, ScalingConfigurationUpdateModelValidator>();

            builder.AddSingleton<CalculationProviderResultsScalingRepository>((ctx) =>
            {
                CosmosDbSettings cosmosDbSettings = new CosmosDbSettings();

                config.Bind("CosmosDbSettings", cosmosDbSettings);

                cosmosDbSettings.ContainerName = "calculationresults";

                CosmosRepository cosmosRepostory = new CosmosRepository(cosmosDbSettings);

                return new CalculationProviderResultsScalingRepository(cosmosRepostory);
            });

            builder.AddSingleton<ProviderSourceDatasetsScalingRepository>((ctx) =>
            {
                CosmosDbSettings cosmosDbSettings = new CosmosDbSettings();

                config.Bind("CosmosDbSettings", cosmosDbSettings);

                cosmosDbSettings.ContainerName = "providerdatasets";

                CosmosRepository cosmosRepository = new CosmosRepository(cosmosDbSettings);

                return new ProviderSourceDatasetsScalingRepository(cosmosRepository);
            });

            builder.AddSingleton<PublishedFundingScalingRepository>((ctx) =>
            {
                CosmosDbSettings cosmosDbSettings = new CosmosDbSettings();

                config.Bind("CosmosDbSettings", cosmosDbSettings);

                cosmosDbSettings.ContainerName = "publishedfunding";

                CosmosRepository cosmosRepository = new CosmosRepository(cosmosDbSettings);

                return new PublishedFundingScalingRepository(cosmosRepository);
            });

            builder.AddSingleton<CalculationsScalingRepository>((ctx) =>
            {
                CosmosDbSettings cosmosDbSettings = new CosmosDbSettings();

                config.Bind("CosmosDbSettings", cosmosDbSettings);

                cosmosDbSettings.ContainerName = "calcs";

                CosmosRepository cosmosRepository = new CosmosRepository(cosmosDbSettings);

                return new CalculationsScalingRepository(cosmosRepository);
            });

            builder.AddSingleton<JobsScalingRepository>((ctx) =>
            {
                CosmosDbSettings cosmosDbSettings = new CosmosDbSettings();

                config.Bind("CosmosDbSettings", cosmosDbSettings);

                cosmosDbSettings.ContainerName = "jobs";

                CosmosRepository cosmosRepository = new CosmosRepository(cosmosDbSettings);

                return new JobsScalingRepository(cosmosRepository);
            });

            builder.AddSingleton<DatasetAggregationsScalingRepository>((ctx) =>
            {
                CosmosDbSettings cosmosDbSettings = new CosmosDbSettings();

                config.Bind("CosmosDbSettings", cosmosDbSettings);

                cosmosDbSettings.ContainerName = "datasetaggregations";

                CosmosRepository cosmosRepository = new CosmosRepository(cosmosDbSettings);

                return new DatasetAggregationsScalingRepository(cosmosRepository);
            });

            builder.AddSingleton<DatasetsScalingRepository>((ctx) =>
            {
                CosmosDbSettings cosmosDbSettings = new CosmosDbSettings();

                config.Bind("CosmosDbSettings", cosmosDbSettings);

                cosmosDbSettings.ContainerName = "datasets";

                CosmosRepository cosmosRepository = new CosmosRepository(cosmosDbSettings);

                return new DatasetsScalingRepository(cosmosRepository);
            });

            builder.AddSingleton<ProfilingScalingRepository>((ctx) =>
            {
                CosmosDbSettings cosmosDbSettings = new CosmosDbSettings();

                config.Bind("CosmosDbSettings", cosmosDbSettings);

                cosmosDbSettings.ContainerName = "profiling";

                CosmosRepository cosmosRepository = new CosmosRepository(cosmosDbSettings);

                return new ProfilingScalingRepository(cosmosRepository);
            });

            builder.AddSingleton<SpecificationsScalingRepository>((ctx) =>
            {
                CosmosDbSettings cosmosDbSettings = new CosmosDbSettings();

                config.Bind("CosmosDbSettings", cosmosDbSettings);

                cosmosDbSettings.ContainerName = "specs";

                CosmosRepository cosmosRepository = new CosmosRepository(cosmosDbSettings);

                return new SpecificationsScalingRepository(cosmosRepository);
            });

            builder.AddSingleton<TestResultsScalingRepository>((ctx) =>
            {
                CosmosDbSettings cosmosDbSettings = new CosmosDbSettings();

                config.Bind("CosmosDbSettings", cosmosDbSettings);

                cosmosDbSettings.ContainerName = "testresults";

                CosmosRepository cosmosRepository = new CosmosRepository(cosmosDbSettings);

                return new TestResultsScalingRepository(cosmosRepository);
            });

            builder.AddSingleton<TestsScalingRepository>((ctx) =>
            {
                CosmosDbSettings cosmosDbSettings = new CosmosDbSettings();

                config.Bind("CosmosDbSettings", cosmosDbSettings);

                cosmosDbSettings.ContainerName = "tests";

                CosmosRepository cosmosRepository = new CosmosRepository(cosmosDbSettings);

                return new TestsScalingRepository(cosmosRepository);
            });

            builder.AddSingleton<UsersScalingRepository>((ctx) =>
            {
                CosmosDbSettings cosmosDbSettings = new CosmosDbSettings();

                config.Bind("CosmosDbSettings", cosmosDbSettings);

                cosmosDbSettings.ContainerName = "users";

                CosmosRepository cosmosRepository = new CosmosRepository(cosmosDbSettings);

                return new UsersScalingRepository(cosmosRepository);
            });

            builder.AddSingleton<ICosmosDbScalingConfigRepository>((ctx) =>
            {
                CosmosDbSettings cosmosDbSettings = new CosmosDbSettings();

                config.Bind("CosmosDbSettings", cosmosDbSettings);

                cosmosDbSettings.ContainerName = "cosmosscalingconfig";

                CosmosRepository cosmosRepository = new CosmosRepository(cosmosDbSettings);

                return new CosmosDbScalingConfigRepository(cosmosRepository);
            });

            builder.AddCaching(config);
           
            Common.Config.ApiClient.Jobs.ServiceCollectionExtensions.AddJobsInterServiceClient(builder, config);

            builder.AddServiceBus(config);

            builder.AddApplicationInsightsTelemetryClient(config, "CalculateFunding.Functions.CosmosDbScaling");

            builder.AddLogging("CalculateFunding.Functions.CosmosDbScaling", config);

            builder.AddTelemetry();

            builder.AddSingleton<ICosmosDbScalingResiliencePolicies>(m =>
            {
                PolicySettings policySettings = builder.GetPolicySettings(config);

                BulkheadPolicy totalNetworkRequestsPolicy = ResiliencePolicyHelpers.GenerateTotalNetworkRequestsPolicy(policySettings);

                CosmosDbScalingResiliencePolicies resiliencePolicies = new CosmosDbScalingResiliencePolicies()
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
