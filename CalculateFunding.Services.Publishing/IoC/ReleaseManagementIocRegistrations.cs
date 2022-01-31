using CalculateFunding.Common.Sql;
using CalculateFunding.Common.Sql.Interfaces;
using CalculateFunding.Common.Storage;
using CalculateFunding.Services.Publishing.FundingManagement;
using CalculateFunding.Services.Publishing.FundingManagement.Interfaces;
using CalculateFunding.Services.Publishing.FundingManagement.ReleaseManagement;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Variations;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace CalculateFunding.Services.Publishing.IoC
{
    public static class ReleaseManagementIocRegistrations
    {
        public static IServiceCollection AddReleaseManagementServices(this IServiceCollection builder, IConfiguration configuration)
        {
            builder.AddSingleton<IExternalApiQueryBuilder, ExternalApiQueryBuilder>();

            builder.AddSingleton<IReleaseManagementRepository, ReleaseManagementRepository>((svc) =>
            {
                ISqlSettings sqlSettings = new SqlSettings();

                configuration.Bind("releaseManagementSql", sqlSettings);
                SqlConnectionFactory factory = new SqlConnectionFactory(sqlSettings);

                SqlPolicyFactory sqlPolicyFactory = new SqlPolicyFactory();
                IExternalApiQueryBuilder externalApiQueryBuilder = svc.GetService<IExternalApiQueryBuilder>();
                return new ReleaseManagementRepository(factory, sqlPolicyFactory, externalApiQueryBuilder);
            });

            builder.AddSingleton<IChannelsService, ChannelsService>();
            builder.AddSingleton<IValidator<ChannelRequest>, ChannelModelValidator>();

            builder.AddScoped<IChannelOrganisationGroupChangeDetector, ChannelOrganisationGroupChangeDetector>();
            builder.AddScoped<IChannelOrganisationGroupGeneratorService, ChannelOrganisationGroupGeneratorService>();
            builder.AddSingleton<IProvidersForChannelFilterService, ProvidersForChannelFilterService>();
            builder.AddScoped<IPublishedProvidersLoadContext, PublishedProvidersLoadContext>();
            builder.AddScoped<IReleaseApprovedProvidersService, ReleaseApprovedProvidersService>();
            builder.AddScoped<IReleaseProvidersToChannelsService, ReleaseProvidersToChannelsService>();
            builder.AddScoped<IChannelReleaseService, ChannelReleaseService>();
            builder.AddScoped<IProviderVersionToChannelReleaseService, ProviderVersionToChannelReleaseService>();
            builder.AddScoped<IReleaseProviderPersistenceService, ReleaseProviderPersistenceService>();
            builder.AddScoped<IReleaseToChannelSqlMappingContext, ReleaseToChannelSqlMappingContext>();
            builder.AddScoped<IProviderVersionReleaseService, ProviderVersionReleaseService>();
            builder.AddScoped<IReleaseManagementSpecificationService, ReleaseManagementSpecificationService>();
            builder.AddScoped<IReleaseProvidersToChannelsService, ReleaseProvidersToChannelsService>();
            builder.AddScoped<IGenerateVariationReasonsForChannelService, GenerateVariationReasonsForChannelService>();
            builder.AddScoped<IProviderVariationReasonsReleaseService, ProviderVariationReasonsReleaseService>();
            builder.AddSingleton<IPublishedProviderChannelVersionService>((ctx) =>
            {
                BlobStorageOptions storageSettings = new BlobStorageOptions();

                configuration.Bind("AzureStorageSettings", storageSettings);

                storageSettings.ContainerName = "releasedproviders";

                IBlobContainerRepository blobContainerRepository = new BlobContainerRepository(storageSettings);
                IBlobClient blobClient = new BlobClient(blobContainerRepository);

                IPublishingResiliencePolicies resiliencePolicies = ctx.GetService<IPublishingResiliencePolicies>();
                ILogger logger = ctx.GetService<ILogger>();

                return new PublishedProviderChannelVersionService(logger, blobClient, resiliencePolicies);
            });

            builder.AddScoped<IPublishedProviderContentChannelPersistenceService, PublishedProviderContentChannelPersistenceService>();
            builder.AddSingleton<IPublishedFundingContentsChannelPersistenceService>((ctx) =>
            {
                BlobStorageOptions storageSettings = new BlobStorageOptions();

                configuration.Bind("AzureStorageSettings", storageSettings);

                storageSettings.ContainerName = "releasedgroups";

                IBlobContainerRepository blobContainerRepository = new BlobContainerRepository(storageSettings);
                IBlobClient blobClient = new BlobClient(blobContainerRepository);

                IPublishedFundingContentsGeneratorResolver resolver = ctx.GetService<IPublishedFundingContentsGeneratorResolver>();
                IPublishingResiliencePolicies resiliencePolicies = ctx.GetService<IPublishingResiliencePolicies>();
                IPublishingEngineOptions engineOptions = ctx.GetService<IPublishingEngineOptions>();
                IPoliciesService policiesService = ctx.GetService<IPoliciesService>();
                ILogger logger = ctx.GetService<ILogger>();

                return new PublishedFundingContentsChannelPersistenceService(logger, resolver, blobClient, resiliencePolicies, engineOptions, policiesService);
            });

            builder.AddScoped<IFundingGroupService, FundingGroupService>();
            builder.AddScoped<IFundingGroupDataGenerator, FundingGroupDataGenerator>();
            builder.AddScoped<IPublishedProviderLoaderForFundingGroupData, PublishedProviderLoaderForFundingGroupData>();
            builder.AddScoped<IFundingGroupDataPersistenceService, FundingGroupDataPersistenceService>();
            builder.AddSingleton<IPublishedProvidersSearchService, PublishedProvidersSearchService>();

            builder.AddTransient<IDetectProviderVariations, ProviderVariationsDetection>();
            builder.AddTransient<IVariationStrategyServiceLocator, VariationStrategyServiceLocator>();

            return builder;
        }
    }
}
