using System;
using CalculateFunding.Common.Config.ApiClient.Jobs;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.Models;
using CalculateFunding.Functions.CosmosDbScaling.ServiceBus;
using CalculateFunding.Models.CosmosDbScaling;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Options;
using CalculateFunding.Services.Core.Services;
using CalculateFunding.Services.CosmosDbScaling;
using CalculateFunding.Services.CosmosDbScaling.Interfaces;
using CalculateFunding.Services.CosmosDbScaling.Repositories;
using CalculateFunding.Services.CosmosDbScaling.Validators;
using FluentValidation;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly.Bulkhead;
using ServiceCollectionExtensions = CalculateFunding.Services.Core.Extensions.ServiceCollectionExtensions;

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
            // These registrations of the functions themselves are just for the DebugQueue. Ideally we don't want these registered in production
            if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
            {
                builder.AddScoped<OnScaleUpCosmosDbCollection>();
            }

            builder.AddSingleton<IUserProfileProvider, UserProfileProvider>();

            builder.AddSingleton<ICosmosDbScalingRepositoryProvider, CosmosDbScalingRepositoryProvider>();

            builder.AddSingleton<ICosmosDbScalingService, CosmosDbScalingService>();

            builder.AddSingleton<ICosmosDbScalingRequestModelBuilder, CosmosDbScalingRequestModelBuilder>();

            builder.AddSingleton<ICosmosDbThrottledEventsFilter, CosmosDbThrottledEventsFilter>();
            builder.AddSingleton<IValidator<ScalingConfigurationUpdateModel>, ScalingConfigurationUpdateModelValidator>();
            builder.AddSingleton<IJobManagement, JobManagement>();

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

            builder.AddScoped<IUserProfileProvider, UserProfileProvider>();

            builder.AddJobsInterServiceClient(config);

            builder.AddServiceBus(config, "cosmosdbscaling");

            builder.AddApplicationInsightsTelemetryClient(config, "CalculateFunding.Functions.CosmosDbScaling");

            builder.AddLogging("CalculateFunding.Functions.CosmosDbScaling", config);

            builder.AddTelemetry();

            builder.AddSingleton<ICosmosDbScalingResiliencePolicies>(m =>
            {
                PolicySettings policySettings = ServiceCollectionExtensions.GetPolicySettings(config);

                AsyncBulkheadPolicy totalNetworkRequestsPolicy = ResiliencePolicyHelpers.GenerateTotalNetworkRequestsPolicy(policySettings);

                CosmosDbScalingResiliencePolicies resiliencePolicies = new CosmosDbScalingResiliencePolicies()
                {
                    ScalingRepository = CosmosResiliencePolicyHelper.GenerateCosmosPolicy(totalNetworkRequestsPolicy),
                    ScalingConfigRepository = CosmosResiliencePolicyHelper.GenerateCosmosPolicy(totalNetworkRequestsPolicy),
                    JobsApiClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                    CacheProvider = ResiliencePolicyHelpers.GenerateRedisPolicy(totalNetworkRequestsPolicy)
                };

                return resiliencePolicies;
            });

            builder.AddSingleton<IJobManagementResiliencePolicies>((ctx) =>
            {
                PolicySettings policySettings = ServiceCollectionExtensions.GetPolicySettings(config);

                AsyncBulkheadPolicy totalNetworkRequestsPolicy = ResiliencePolicyHelpers.GenerateTotalNetworkRequestsPolicy(policySettings);
                
                return new JobManagementResiliencePolicies()
                {
                    JobsApiClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                };

            });

            return builder.BuildServiceProvider();
        }
    }
}
