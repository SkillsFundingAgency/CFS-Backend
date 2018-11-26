using System;
using CalculateFunding.Models.Jobs;
using CalculateFunding.Repositories.Common.Cosmos;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Options;
using CalculateFunding.Services.Jobs;
using CalculateFunding.Services.Jobs.Interfaces;
using CalculateFunding.Services.Jobs.Repositories;
using CalculateFunding.Services.Jobs.Validators;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly.Bulkhead;

namespace CalculateFunding.Functions.Jobs
{
    public static class IocConfig
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

        public static IServiceProvider BuildServiceProvider(IConfigurationRoot config)
        {
            ServiceCollection serviceProvider = new ServiceCollection();

            RegisterComponents(serviceProvider, config);

            return serviceProvider.BuildServiceProvider();
        }

        static public void RegisterComponents(IServiceCollection builder, IConfigurationRoot config)
        {
            builder
                .AddSingleton<IJobManagementService, JobManagementService>();

            builder.
                AddSingleton<IValidator<CreateJobValidationModel>, CreateJobValidator>();

            builder
                .AddSingleton<IJobRepository, JobRepository>((ctx) =>
                {
                    CosmosDbSettings cosmosDbSettings = new CosmosDbSettings();

                    config.Bind("CosmosDbSettings", cosmosDbSettings);

                    cosmosDbSettings.CollectionName = "jobs";

                    CosmosRepository jobCosmosRepostory = new CosmosRepository(cosmosDbSettings);

                    return new JobRepository(jobCosmosRepostory);
                });

            builder.AddServiceBus(config);

            builder.AddPolicySettings(config);

            builder.AddSingleton<IJobsResiliencePolicies>((ctx) =>
            {
                PolicySettings policySettings = ctx.GetService<PolicySettings>();

                BulkheadPolicy totalNetworkRequestsPolicy = ResiliencePolicyHelpers.GenerateTotalNetworkRequestsPolicy(policySettings);

                return new ResiliencePolicies
                {
                    JobDefinitionsRepository = CosmosResiliencePolicyHelper.GenerateCosmosPolicy(totalNetworkRequestsPolicy),
                    CacheProviderPolicy = ResiliencePolicyHelpers.GenerateRedisPolicy(totalNetworkRequestsPolicy),
                    JobRepository = CosmosResiliencePolicyHelper.GenerateCosmosPolicy(totalNetworkRequestsPolicy),
                    JobRepositoryNonAsync = CosmosResiliencePolicyHelper.GenerateCosmosPolicy(totalNetworkRequestsPolicy),
                    MessengerServicePolicy = ResiliencePolicyHelpers.GenerateMessagingPolicy(totalNetworkRequestsPolicy)
                };
            });

            builder.AddApplicationInsightsTelemetryClient(config, "CalculateFunding.Functions.Jobs");

            builder.AddLogging("CalculateFunding.Functions.Jobs");

            builder.AddTelemetry();
        }
    }
}
