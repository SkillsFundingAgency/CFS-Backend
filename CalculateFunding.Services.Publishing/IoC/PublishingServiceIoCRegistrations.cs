using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Common.Storage;
using CalculateFunding.Generators.OrganisationGroup;
using CalculateFunding.Generators.OrganisationGroup.Interfaces;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Options;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Providers;
using CalculateFunding.Services.Publishing.Specifications;
using CalculateFunding.Services.Publishing.Validators;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly.Bulkhead;

namespace CalculateFunding.Services.Publishing.IoC
{
    public static class PublishingServiceIoCRegistrations
    {
        public static IServiceCollection AddPublishingServices(this IServiceCollection serviceCollection, IConfiguration configuration)
        {
            RegisterSpecificationServiceComponents(serviceCollection, configuration);

            return serviceCollection;
        }

        private static void RegisterSpecificationServiceComponents(IServiceCollection serviceCollection, IConfiguration configuration)
        {
            serviceCollection.AddSingleton(ctx => configuration);
            serviceCollection.AddScoped<ISpecificationPublishingService, SpecificationPublishingService>();
            serviceCollection.AddSingleton<IProviderFundingPublishingService, ProviderFundingPublishingService>();
            serviceCollection.AddSingleton<IHealthChecker, ProviderFundingPublishingService>();
            serviceCollection.AddSingleton<ISpecificationIdServiceRequestValidator, PublishSpecificationValidator>();
            serviceCollection.AddSingleton<IPublishedProviderIdsServiceRequestValidator, PublishedProviderIdsValidator>();
            serviceCollection.AddSingleton<IPublishedProviderFundingService, PublishedProviderFundingService>();
            serviceCollection.AddSingleton<ISpecificationService, SpecificationService>();
            serviceCollection.AddSingleton<IProviderService, ProviderService>();
            serviceCollection.AddSingleton<IPublishedProviderIndexerService, PublishedProviderIndexerService>();
            serviceCollection.AddSingleton<IPublishProviderExclusionCheck, PublishedProviderExclusionCheck>();
            serviceCollection.AddSingleton<IFundingLineValueOverride, FundingLineValueOverride>();
            serviceCollection.AddSingleton<IPublishedFundingDateService, PublishedFundingDateService>();
            serviceCollection.AddSingleton<IPublishedFundingDataService, PublishedFundingDataService>();
            serviceCollection.AddSingleton<IPublishedProviderContentPersistanceService, PublishedProviderContentPersistanceService>();
            serviceCollection.AddSingleton<ICreateRefreshFundingJobs, RefreshFundingJobCreation>();
            serviceCollection.AddSingleton<ICreateApproveAllFundingJobs, ApproveAllFundingJobCreation>();
            serviceCollection.AddSingleton<ICreateApproveBatchFundingJobs, ApproveBatchFundingJobCreation>();
            serviceCollection.AddSingleton<ICreateAllPublishProviderFundingJobs, AllPublishProviderFundingJobCreation>();
            serviceCollection.AddSingleton<ICreateBatchPublishProviderFundingJobs, BatchPublishProviderFundingJobCreation>();
            serviceCollection.AddSingleton<ICreateDeleteSpecificationJobs, DeleteSpecificationJobCreation>();
            serviceCollection.AddSingleton<ICreateDeletePublishedProvidersJobs, DeletePublishedProvidersJobCreation>();
            serviceCollection.AddSingleton<IPublishedFundingStatusUpdateService, PublishedFundingStatusUpdateService>();
            serviceCollection.AddSingleton<IDeleteSpecifications, DeleteSpecificationService>();
            serviceCollection.AddSingleton<IDeletePublishedProvidersService, DeletePublishedProvidersService>();
            serviceCollection.AddSingleton<IDeleteFundingSearchDocumentsService, DeleteFundingSearchDocumentsService>();
            serviceCollection.AddSingleton<IDeselectSpecificationForFundingService, DeselectSpecificationForFundingService>();
            serviceCollection.AddSingleton<IDeletePublishedFundingBlobDocumentsService, DeletePublishedFundingBlobDocumentsService>();

            PolicySettings policySettings = ServiceCollectionExtensions.GetPolicySettings(configuration);
            OrganisationGroupResiliencePolicies organisationResiliencePolicies = CreateResiliencePolicies(policySettings);

            serviceCollection.AddSingleton<IOrganisationGroupResiliencePolicies>(organisationResiliencePolicies);
            serviceCollection.AddSingleton<IOrganisationGroupTargetProviderLookup, OrganisationGroupTargetProviderLookup>();
            serviceCollection.AddSingleton<IOrganisationGroupGenerator, OrganisationGroupGenerator>();

            serviceCollection.AddSingleton<IJobsRunning, JobsRunning>();
            serviceCollection.AddSingleton<ICalculationPrerequisiteCheckerService, CalculationPrerequisiteCheckerService>();

            serviceCollection.AddScoped<IPrerequisiteCheckerLocator, PrerequisiteCheckerLocator>();
            serviceCollection.AddScoped<IPrerequisiteChecker, RefreshPrerequisiteChecker>();
            serviceCollection.AddScoped<IPrerequisiteChecker, PublishAllPrerequisiteChecker>();
            serviceCollection.AddScoped<IPrerequisiteChecker, PublishBatchPrerequisiteChecker>();
            serviceCollection.AddScoped<IPrerequisiteChecker, ApproveAllProvidersPrerequisiteChecker>();
            serviceCollection.AddScoped<IPrerequisiteChecker, ApproveBatchProvidersPrerequisiteChecker>();

            serviceCollection.AddSingleton<IPublishedFundingChangeDetectorService, PublishedFundingChangeDetectorService>();

            serviceCollection.AddSingleton<IPublishedFundingGenerator, PublishedFundingGenerator>();

            SearchRepositorySettings searchSettings = new SearchRepositorySettings
            {
                SearchServiceName = configuration.GetValue<string>("SearchServiceName"),
                SearchKey = configuration.GetValue<string>("SearchServiceKey")
            };

            serviceCollection.AddSingleton(searchSettings);
            serviceCollection.AddSingleton<ISearchRepository<PublishedFundingIndex>, SearchRepository<PublishedFundingIndex>>();
            serviceCollection
                .AddSingleton<IBlobContainerRepository, BlobContainerRepository>();
            serviceCollection.AddSingleton<IPublishedFundingContentsPersistanceService>((ctx) =>
            {
                BlobStorageOptions storageSettings = new BlobStorageOptions();

                configuration.Bind("AzureStorageSettings", storageSettings);

                storageSettings.ContainerName = "publishedfunding";

                IBlobClient blobClient = new BlobClient(storageSettings, ctx.GetService<IBlobContainerRepository>());

                IPublishedFundingContentsGeneratorResolver publishedFundingContentsGeneratorResolver = ctx.GetService<IPublishedFundingContentsGeneratorResolver>();

                ISearchRepository<PublishedFundingIndex> searchRepository = ctx.GetService<ISearchRepository<PublishedFundingIndex>>();

                IPublishingResiliencePolicies publishingResiliencePolicies = ctx.GetService<IPublishingResiliencePolicies>();

                return new PublishedFundingContentsPersistanceService(publishedFundingContentsGeneratorResolver,
                    blobClient,
                    publishingResiliencePolicies,
                    ctx.GetService<IPublishingEngineOptions>());
            });
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