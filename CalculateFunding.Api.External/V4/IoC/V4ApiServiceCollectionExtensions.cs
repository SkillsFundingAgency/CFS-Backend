using AutoMapper;
using CalculateFunding.Api.External.V4.Interfaces;
using CalculateFunding.Api.External.V4.MappingProfiles;
using CalculateFunding.Api.External.V4.Services;
using CalculateFunding.Common.Storage;
using CalculateFunding.Services.Core.Caching.FileSystem;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Options;
using CalculateFunding.Services.Publishing.FundingManagement.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly.Bulkhead;
using Serilog;

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

            MapperConfiguration externalConfig = new MapperConfiguration(c =>
            {
                c.AddProfile<ExternalServiceMappingProfile>();
            });

            builder.AddSingleton(externalConfig.CreateMapper());

            return builder;
        }
    }
}
