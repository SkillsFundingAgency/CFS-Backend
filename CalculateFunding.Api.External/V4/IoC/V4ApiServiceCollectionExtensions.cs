using AutoMapper;
using CalculateFunding.Api.External.V4.Interfaces;
using CalculateFunding.Api.External.V4.MappingProfiles;
using CalculateFunding.Api.External.V4.Services;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.Storage;
using CalculateFunding.Generators.OrganisationGroup;
using CalculateFunding.Generators.OrganisationGroup.Interfaces;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Core.Caching.FileSystem;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Services.Core.Interfaces.Services;
using CalculateFunding.Services.Core.Options;
using CalculateFunding.Services.Core.Services;
using CalculateFunding.Services.Publishing;
using CalculateFunding.Services.Publishing.FundingManagement.Interfaces;
using CalculateFunding.Services.Publishing.FundingManagement.ReleaseManagement;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Providers;
using CalculateFunding.Services.Publishing.Reporting;
using CalculateFunding.Services.Publishing.Repositories;
using CalculateFunding.Services.Publishing.Specifications;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly.Bulkhead;
using Serilog;
using System;
using ServiceCollectionExtensions = CalculateFunding.Services.Core.Extensions.ServiceCollectionExtensions;

namespace CalculateFunding.Api.External.V4.IoC
{
    public static class V4ApiServiceCollectionExtensions
    {
        public static IServiceCollection AddExternalApiV4Services(this IServiceCollection builder, IConfiguration configuration)
        {
            builder.AddSingleton<IFundingFeedServiceV4, FundingFeedServiceV4>();
            builder.AddSingleton<IChannelUrlToIdResolver, ChannelUrlToIdResolver>();
            builder.AddSingleton<IBlobDocumentPathGenerator, BlobDocumentPathGenerator>();
            builder.AddSingleton<IExternalApiFeedWriter, ExternalApiFeedWriter>();
            builder.AddSingleton<IFundingFeedItemByIdService, FundingFeedItemByIdService>();
            builder.AddSingleton<IPublishedProviderRetrievalService, PublishedProviderRetrievalService>();
            builder.AddSingleton<IFundingStreamService, FundingStreamService>();
            builder.AddSingleton<IBlobClient, BlobClient>((ctx) =>
                {
                    BlobStorageOptions storageSettings = new BlobStorageOptions();

                    configuration.Bind("AzureStorageSettings", storageSettings);

                    storageSettings.ContainerName = "publishedproviderversions";

                    IBlobContainerRepository blobContainerRepository = new BlobContainerRepository(storageSettings);
                    return new BlobClient(blobContainerRepository);
                });

            builder.AddSingleton<IProviderFundingVersionService>((ctx) =>
            {
                BlobStorageOptions storageSettings = new BlobStorageOptions();

                configuration.Bind("AzureStorageSettings", storageSettings);

                storageSettings.ContainerName = "publishedproviderversions";

                IBlobContainerRepository blobContainerRepository = new BlobContainerRepository(storageSettings);
                IBlobClient blobClient = new BlobClient(blobContainerRepository);

                IExternalApiResiliencePolicies publishingResiliencePolicies = ctx.GetService<IExternalApiResiliencePolicies>();
                ILogger logger = ctx.GetService<ILogger>();
                IFileSystemCache fileSystemCache = ctx.GetService<IFileSystemCache>();
                IExternalApiFileSystemCacheSettings settings = ctx.GetService<IExternalApiFileSystemCacheSettings>();
                IReleaseManagementRepository releaseManagementRepository = ctx.GetService<IReleaseManagementRepository>();
                IChannelUrlToIdResolver channelUrlToIdResolver = ctx.GetService<IChannelUrlToIdResolver>();

                return new ProviderFundingVersionService(blobClient, releaseManagementRepository, channelUrlToIdResolver, logger, publishingResiliencePolicies, fileSystemCache, settings);
            });
            
            builder.AddSingleton<IPublishedFundingBulkRepository, PublishedFundingBulkRepository>((ctx) =>
            {
                CosmosDbSettings settings = new CosmosDbSettings();

                configuration.Bind("CosmosDbSettings", settings);

                settings.ContainerName = "publishedfunding";

                IPublishingEngineOptions publishingEngineOptions = ctx.GetService<IPublishingEngineOptions>();

                CosmosRepository calcsCosmosRepository = new CosmosRepository(settings, publishingEngineOptions.AllowBatching ? new CosmosClientOptions
                {
                    ConnectionMode = ConnectionMode.Direct,
                    RequestTimeout = new TimeSpan(0, 0, 15),
                    MaxRequestsPerTcpConnection = publishingEngineOptions.MaxRequestsPerTcpConnectionPublishedFundingCosmosBulkOptions,
                    MaxTcpConnectionsPerEndpoint = 4,
                    ConsistencyLevel = ConsistencyLevel.Eventual,
                    AllowBulkExecution = true
                } : null);

                IPublishingResiliencePolicies publishingResiliencePolicies = ctx.GetService<IPublishingResiliencePolicies>();

                return new PublishedFundingBulkRepository(publishingResiliencePolicies, publishingEngineOptions, calcsCosmosRepository);
            });

            builder.AddSingleton<IPublishedFundingRetrievalService>((ctx) =>
            {
                BlobStorageOptions storageSettings = new BlobStorageOptions();

                configuration.Bind("AzureStorageSettings", storageSettings);

                storageSettings.ContainerName = "publishedfunding";

                IBlobContainerRepository blobContainerRepository = new BlobContainerRepository(storageSettings);
                IBlobClient blobClient = new BlobClient(blobContainerRepository);

                IExternalApiResiliencePolicies resiliencePolicies = ctx.GetService<IExternalApiResiliencePolicies>();
                ILogger logger = ctx.GetService<ILogger>();
                IFileSystemCache fileSystemCache = ctx.GetService<IFileSystemCache>();
                IExternalApiFileSystemCacheSettings settings = ctx.GetService<IExternalApiFileSystemCacheSettings>();

                IBlobDocumentPathGenerator blobDocumentPathGenerator = ctx.GetService<IBlobDocumentPathGenerator>();
                IExternalEngineOptions externalEngineOptions = ctx.GetService<IExternalEngineOptions>();

                return new PublishedFundingRetrievalService(blobClient,
                                                            resiliencePolicies,
                                                            fileSystemCache,
                                                            blobDocumentPathGenerator,
                                                            logger,
                                                            settings,
                                                            externalEngineOptions);
            });

            builder.AddSingleton<IExternalApiFileSystemCacheSettings>(ctx =>
            {
                ExternalApiFileSystemCacheSettings settings = new ExternalApiFileSystemCacheSettings();

                configuration.Bind("externalapifilesystemcachesettings", settings);

                return settings;
            });

            builder.AddSingleton<IExternalEngineOptions>(ctx =>
            {
                ExternalEngineOptions settings = new ExternalEngineOptions();

                configuration.Bind("externalengineoptions", settings);

                return settings;
            });

            builder.AddSingleton<IExternalApiResiliencePolicies>((ctx) =>
            {
                PolicySettings policySettings = ctx.GetService<PolicySettings>();

                AsyncBulkheadPolicy totalNetworkRequestsPolicy = ResiliencePolicyHelpers.GenerateTotalNetworkRequestsPolicy(policySettings);

                return new ExternalApiResiliencePolicies()
                {
                    PublishedProviderBlobRepositoryPolicy = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                    PublishedFundingBlobRepositoryPolicy = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                    PoliciesApiClientPolicy = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                };
            });

            PolicySettings policySettings = ServiceCollectionExtensions.GetPolicySettings(configuration);
            OrganisationGroupResiliencePolicies organisationResiliencePolicies = CreateResiliencePolicies(policySettings);

            builder.AddSingleton<IOrganisationGroupResiliencePolicies>(organisationResiliencePolicies);
            builder.AddSingleton<IOrganisationGroupGenerator, OrganisationGroupGenerator>();
            builder.AddSingleton<IOrganisationGroupTargetProviderLookup, OrganisationGroupTargetProviderLookup>();
            builder.AddSingleton<IProviderFilter, ProviderFilter>();
            builder.AddScoped<IPublishService, PublishService>();
            builder.AddSingleton<IPublishingEngineOptions>(_ => new PublishingEngineOptions(configuration));


            builder.AddSingleton<IVersionRepository<PublishedFundingVersion>, VersionRepository<PublishedFundingVersion>>((ctx) =>
            {
                CosmosDbSettings ProviderSourceDatasetVersioningDbSettings = new CosmosDbSettings();

                configuration.Bind("CosmosDbSettings", ProviderSourceDatasetVersioningDbSettings);

                ProviderSourceDatasetVersioningDbSettings.ContainerName = "publishedfunding";

                CosmosRepository cosmosRepository = new CosmosRepository(ProviderSourceDatasetVersioningDbSettings);

                return new VersionRepository<PublishedFundingVersion>(cosmosRepository, new NewVersionBuilderFactory<PublishedFundingVersion>());
            });

            builder.AddSingleton<IVersionBulkRepository<PublishedFundingVersion>, VersionBulkRepository<PublishedFundingVersion>>((ctx) =>
            {
                CosmosDbSettings PublishedFundingVersioningDbSettings = new CosmosDbSettings();

                configuration.Bind("CosmosDbSettings", PublishedFundingVersioningDbSettings);

                PublishedFundingVersioningDbSettings.ContainerName = "publishedfunding";

                IPublishingEngineOptions publishingEngineOptions = ctx.GetService<IPublishingEngineOptions>();

                CosmosRepository cosmosRepository = new CosmosRepository(PublishedFundingVersioningDbSettings, publishingEngineOptions.AllowBatching ? new CosmosClientOptions
                {
                    ConnectionMode = ConnectionMode.Direct,
                    RequestTimeout = new TimeSpan(0, 0, 15),
                    MaxRequestsPerTcpConnection = publishingEngineOptions.MaxRequestsPerTcpConnectionPublishedFundingCosmosBulkOptions,
                    MaxTcpConnectionsPerEndpoint = publishingEngineOptions.MaxTcpConnectionsPerEndpointPublishedFundingCosmosBulkOptions,
                    ConsistencyLevel = ConsistencyLevel.Eventual,
                    AllowBulkExecution = true
                } : null);

                return new VersionBulkRepository<PublishedFundingVersion>(cosmosRepository, new NewVersionBuilderFactory<PublishedFundingVersion>());
            });
            builder.AddSingleton<ITransactionResiliencePolicies>((ctx) => new TransactionResiliencePolicies()
            {
                TransactionPolicy = ResiliencePolicyHelpers.GenerateTotalNetworkRequestsPolicy(policySettings)
            });
            builder.AddSingleton<ICreateProcessDatasetObsoleteItemsJob, ProcessDatasetObsoleteItemsJobCreation>();
            builder.AddSingleton<ICreatePublishDatasetsDataCopyJob, PublishingDatasetsDataCopyJobCreation>();
            builder.AddScoped<IGeneratePublishedFundingCsvJobsCreationLocator, GeneratePublishedFundingCsvJobsCreationLocator>();
            builder.AddScoped<IPublishedFundingCsvJobsService, PublishedFundingCsvJobsService>();
            builder.AddSingleton<ICreatePublishIntegrityJob, PublishIntegrityCheckJobCreation>();
            builder.AddSingleton<IPublishedFundingDateService, PublishedFundingDateService>();
            builder.AddSingleton<IPublishedFundingService, PublishedFundingService>();
            builder.AddSingleton<ITransactionFactory, TransactionFactory>();
            builder.AddSingleton<IPublishedFundingDataService, PublishedFundingDataService>();
            builder.AddSingleton<IPoliciesService, PoliciesService>();
            builder.AddSingleton<IProviderService, ProviderService>();
            builder.AddSingleton<IPublishedProviderStatusUpdateService, PublishedProviderStatusUpdateService>();
            builder.AddSingleton<IPublishedProviderIndexerService, PublishedProviderIndexerService>();
            builder.AddSingleton<ISearchRepository<PublishedProviderIndex>, SearchRepository<PublishedProviderIndex>>();
            builder.AddSingleton<IPublishedProviderVersioningService, PublishedProviderVersioningService>();
            builder.AddSingleton<IPublishedProviderVersionService, PublishedProviderVersionService>();
            builder.AddSingleton<IPublishedFundingGenerator, PublishedFundingGenerator>();
            builder.AddSingleton<IPublishedFundingChangeDetectorService, PublishedFundingChangeDetectorService>();
            builder.AddScoped<IPrerequisiteCheckerLocator, PrerequisiteCheckerLocator>();
            builder.AddSingleton<ISpecificationService, SpecificationService>();
            builder.AddSingleton<IPublishedFundingStatusUpdateService, PublishedFundingStatusUpdateService>();
            builder.AddScoped<IPublishedProviderChannelVersionService>((ctx) =>
            {
                BlobStorageOptions storageSettings = new BlobStorageOptions();

                configuration.Bind("AzureStorageSettings", storageSettings);

                storageSettings.ContainerName = "releasedgroups";

                IBlobContainerRepository blobContainerRepository = new BlobContainerRepository(storageSettings);

                IBlobClient blobClient = new BlobClient(blobContainerRepository);

                ILogger logger = ctx.GetService<ILogger>();

                IPublishingResiliencePolicies publishingResiliencePolicies = ctx.GetService<IPublishingResiliencePolicies>();

                return new PublishedProviderChannelVersionService(logger,
                    blobClient,
                    publishingResiliencePolicies);
            });

            builder.AddSingleton<ICalculationsService, CalculationsService>();
            builder.AddScoped<IPublishedProviderContentChannelPersistenceService, PublishedProviderContentChannelPersistenceService>();
            builder.AddScoped<IPublishedFundingContentsChannelPersistenceService>((ctx) =>
            {
                BlobStorageOptions storageSettings = new BlobStorageOptions();

                configuration.Bind("AzureStorageSettings", storageSettings);

                storageSettings.ContainerName = "releasedgroups";

                IBlobContainerRepository blobContainerRepository = new BlobContainerRepository(storageSettings);

                IBlobClient blobClient = new BlobClient(blobContainerRepository);

                IPublishedFundingContentsGeneratorResolver publishedFundingContentsGeneratorResolver = ctx.GetService<IPublishedFundingContentsGeneratorResolver>();

                ILogger logger = ctx.GetService<ILogger>();

                IPoliciesService policiesService = ctx.GetService<IPoliciesService>();

                IPublishingResiliencePolicies publishingResiliencePolicies = ctx.GetService<IPublishingResiliencePolicies>();

                return new PublishedFundingContentsChannelPersistenceService(logger,
                    publishedFundingContentsGeneratorResolver,
                    blobClient,
                    publishingResiliencePolicies,
                    ctx.GetService<IPublishingEngineOptions>(),
                    policiesService);
            });

            return builder;
        }

        private static OrganisationGroupResiliencePolicies CreateResiliencePolicies(PolicySettings policySettings)
        {
            AsyncBulkheadPolicy totalNetworkRequestsPolicy = ResiliencePolicyHelpers.GenerateTotalNetworkRequestsPolicy(policySettings);

            OrganisationGroupResiliencePolicies resiliencePolicies = new OrganisationGroupResiliencePolicies
            {
                ProvidersApiClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy)
            };

            return resiliencePolicies;
        }
    }
}
