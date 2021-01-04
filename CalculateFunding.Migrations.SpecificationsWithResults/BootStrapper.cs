using System;
using System.Threading;
using CalculateFunding.Common.Config.ApiClient.Jobs;
using CalculateFunding.Common.Config.ApiClient.Policies;
using CalculateFunding.Common.Config.ApiClient.Results;
using CalculateFunding.Common.Config.ApiClient.Specifications;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.Models;
using CalculateFunding.Migrations.SpecificationsWithResults.Migrations;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Interfaces.Threading;
using CalculateFunding.Services.Core.Options;
using CalculateFunding.Services.Core.Threading;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Results;
using CalculateFunding.Services.Results.Interfaces;
using CalculateFunding.Services.Results.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly.Bulkhead;
using ICalculationResultsRepository = CalculateFunding.Services.Results.Interfaces.ICalculationResultsRepository;
using ResiliencePolicies = CalculateFunding.Services.Publishing.ResiliencePolicies;

namespace CalculateFunding.Migrations.SpecificationsWithResults
{
    public class BootStrapper
    {
        private static readonly IConfigurationRoot Configuration = new ConfigurationBuilder()
            .AddUserSecrets("df0d69d5-a6db-4598-909f-262fc39cb8c8")
            .Build();

        public static IServiceProvider BuildServiceProvider()
        {
            IServiceCollection serviceCollection = new ServiceCollection();

            serviceCollection.AddApplicationInsightsTelemetryClient(Configuration, "CalculateFunding.Migrations.SpecificationsWithResults");
            serviceCollection.AddLogging(serviceName: "CalculateFunding.Migrations.SpecificationsWithResults");
            serviceCollection.AddSingleton<ISpecificationsWithProviderResultsService, SpecificationsWithProviderResultsService>();
            serviceCollection.AddSingleton<IProducerConsumerFactory, ProducerConsumerFactory>();
            serviceCollection.AddPolicySettings(Configuration);
            serviceCollection.AddResultsInterServiceClient(Configuration, handlerLifetime: Timeout.InfiniteTimeSpan);
            serviceCollection.AddSpecificationsInterServiceClient(Configuration, handlerLifetime: Timeout.InfiniteTimeSpan);
            serviceCollection.AddPoliciesInterServiceClient(Configuration, handlerLifetime: Timeout.InfiniteTimeSpan);
            serviceCollection.AddSingleton<IJobManagement, JobManagement>();
            serviceCollection.AddJobsInterServiceClient(Configuration, handlerLifetime: Timeout.InfiniteTimeSpan);
            serviceCollection.AddSingleton<ICosmosRepository>(ctx =>
            {
                CosmosDbSettings cosmosDbSettings = new CosmosDbSettings();

                Configuration.Bind("CosmosDbSettings", cosmosDbSettings);

                cosmosDbSettings.ContainerName = "calculationresults";

                return new CosmosRepository(cosmosDbSettings);
            });
            serviceCollection.AddSingleton<ICalculationResultsRepository, CalculationResultsRepository>();
            serviceCollection.AddSingleton<IPublishingResiliencePolicies>(ctx =>
            {
                PolicySettings policySettings = ctx.GetService<PolicySettings>();
                AsyncBulkheadPolicy totalNetworkRequestsPolicy = ResiliencePolicyHelpers
                    .GenerateTotalNetworkRequestsPolicy(policySettings);

                return new ResiliencePolicies
                {
                    PublishedProviderVersionRepository = ResiliencePolicyHelpers
                        .GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                    SpecificationsApiClient = ResiliencePolicyHelpers
                        .GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                };
            });
            serviceCollection.AddSingleton<IResultsResiliencePolicies>(ctx =>
            {
                PolicySettings policySettings = ctx.GetService<PolicySettings>();
                AsyncBulkheadPolicy totalNetworkRequestsPolicy = ResiliencePolicyHelpers
                    .GenerateTotalNetworkRequestsPolicy(policySettings);

                return new Services.Results.ResiliencePolicies
                {
                    CalculationProviderResultsSearchRepository = ResiliencePolicyHelpers
                        .GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                    JobsApiClient = ResiliencePolicyHelpers
                        .GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                    PoliciesApiClient = ResiliencePolicyHelpers
                        .GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy)
                };
            });

            serviceCollection.AddTransient<IMergeSpecificationsWithProviderResultsDocuments, MergeSpecificationsWithProviderResultsDocuments>();
            serviceCollection.AddSingleton<IUserProfileProvider, UserProfileProvider>();
            serviceCollection.AddSingleton<IJobManagementResiliencePolicies>(ctx =>
            {
                PolicySettings policySettings = ctx.GetService<PolicySettings>();
                AsyncBulkheadPolicy totalNetworkRequestsPolicy = ResiliencePolicyHelpers
                    .GenerateTotalNetworkRequestsPolicy(policySettings);

                return new JobManagementResiliencePolicies
                {
                    JobsApiClient = ResiliencePolicyHelpers
                        .GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy)
                };
            });
            serviceCollection.AddServiceBus(Configuration);

            return serviceCollection.BuildServiceProvider();
        }
    }
}