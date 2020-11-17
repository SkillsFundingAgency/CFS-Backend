using System;
using System.Threading;
using CalculateFunding.Common.Config.ApiClient.Jobs;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Storage;
using CalculateFunding.Migrations.DSG.RollBack.Migrations;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Interfaces.Threading;
using CalculateFunding.Services.Core.Options;
using CalculateFunding.Services.Core.Threading;
using CalculateFunding.Services.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Interfaces.Undo;
using CalculateFunding.Services.Publishing.Undo;
using CalculateFunding.Services.Publishing.Undo.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly.Bulkhead;
using Serilog;
using CommonBlobClient = CalculateFunding.Common.Storage.BlobClient;

namespace CalculateFunding.Migrations.DSG.RollBack
{
    public class BootStrapper
    {
        private static readonly string ServiceName = typeof(BootStrapper)
            .Assembly
            .GetName()
            .Name;
        
        private static readonly IConfigurationRoot Configuration = ConfigHelper.AddConfig();

        public static IServiceProvider BuildServiceProvider(string collectionName)
        {
            IServiceCollection builder = new ServiceCollection();

            builder.AddApplicationInsightsTelemetryClient(Configuration, ServiceName);
            builder.AddLogging(serviceName: ServiceName);
            builder.AddSingleton<IProducerConsumerFactory, ProducerConsumerFactory>();
            builder.AddPolicySettings(Configuration);
            builder.AddSingleton<IJobManagement, JobManagement>();
            builder.AddSingleton<IJobTracker, JobTracker>();
            builder.AddJobsInterServiceClient(Configuration, handlerLifetime: Timeout.InfiniteTimeSpan);
            builder.AddServiceBus(Configuration);
            builder.AddSingleton<IUserProfileProvider, UserProfileProvider>();
            builder.AddSingleton<ICosmosRepository>(ctx =>
            {
                CosmosDbSettings cosmosDbSettings = new CosmosDbSettings();

                Configuration.Bind("CosmosDbSettings", cosmosDbSettings);

                cosmosDbSettings.ContainerName = collectionName;

                return new CosmosRepository(cosmosDbSettings);
            });
            builder.AddSingleton<IPublishingResiliencePolicies>(ctx =>
            {
                PolicySettings policySettings = ctx.GetService<PolicySettings>();
                AsyncBulkheadPolicy totalNetworkRequestsPolicy = ResiliencePolicyHelpers
                    .GenerateTotalNetworkRequestsPolicy(policySettings);
                
                return new ResiliencePolicies
                {
                    JobsApiClient = ResiliencePolicyHelpers
                        .GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                    PublishedFundingRepository = ResiliencePolicyHelpers
                        .GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                    PublishedIndexSearchResiliencePolicy = ResiliencePolicyHelpers
                        .GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                    BlobClient = ResiliencePolicyHelpers
                        .GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy)
                };
            });

            builder.AddSingleton<IJobManagementResiliencePolicies>(ctx =>
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
            builder.AddSingleton<IPublishedFundingUndoCosmosRepository, PublishedFundingUndoCosmosRepository>();
            builder.AddSingleton<IPublishedFundingUndoBlobStoreRepository>(ctx =>
            {
                BlobStorageOptions settings = new BlobStorageOptions();

                Configuration.Bind("AzureStorageSettings", settings);

                settings.ContainerName = "publishedproviderversions";

                IBlobContainerRepository blobContainerRepository = new BlobContainerRepository(settings);
                return new PublishedFundingUndoBlobStoreRepository(new CommonBlobClient(blobContainerRepository),
                    ctx.GetService<IPublishingResiliencePolicies>(),
                    ctx.GetService<ILogger>());
            });
            builder.AddSingleton<IPublishedFundingUndoJobCreation, PublishedFundingUndoJobCreation>();
            builder.AddSingleton<IPublishedFundingUndoTaskFactoryLocator, PublishedFundingUndoTaskFactoryLocator>();
            builder.AddSingleton<IDsgRollBackCosmosDocumentsJob, DsgRollBackCosmosDocumentsJob>();
            builder.AddSingleton<IPublishedFundingUndoTaskFactory, DsgRollBackTaskFactory>();
            builder.AddSingleton<IRollBackDsg, RollBackDsg>();

            return builder.BuildServiceProvider();
        }
    }
}