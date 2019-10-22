using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using CalculateFunding.Common.ApiClient.Calcs.Models;
using CalculateFunding.Common.ApiClient.Jobs;
using CalculateFunding.Generators.OrganisationGroup;
using CalculateFunding.Generators.OrganisationGroup.Interfaces;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Publishing.AcceptanceTests.Repositories;
using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Services.Core.Services;
using CalculateFunding.Services.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;
using Microsoft.Azure.ServiceBus;
using Polly;
using Serilog;

namespace CalculateFunding.Publishing.AcceptanceTests.Contexts
{
    public class PublishServiceAcceptanceStepContext : IPublishFundingStepContext
    {
        private readonly IJobStepContext _jobStepContext;
        private readonly ICurrentSpecificationStepContext _currentSpecificationStepContext;
        private readonly IPublishedFundingRepositoryStepContext _publishedFundingRepositoryStepContext;
        private readonly IPoliciesStepContext _policiesStepContext;
        private readonly IProvidersStepContext _providersStepContext;
        private readonly IPublishedProviderStepContext _publishedProviderStepContext;
        private readonly IPublishingDatesStepContext _publishingDatesStepContext;
        private readonly ILoggerStepContext _loggerStepContext;

        public IEnumerable<CalculationResult> CalculationResults { get; set; }

        public IEnumerable<CalculationMetadata> CalculationMetadata { get; set; }

        public TemplateMapping TemplateMapping { get; set; }

        public PublishServiceAcceptanceStepContext(IJobStepContext jobStepContext,
            ICurrentSpecificationStepContext currentSpecificationStepContext,
            IPublishedFundingRepositoryStepContext publishedFundingRepositoryStepContext,
            IPoliciesStepContext policiesStepContext,
            IProvidersStepContext providersStepContext,
            IPublishingDatesStepContext publishingDatesStepContext,
            ILoggerStepContext loggerStepContext,
            IPublishedProviderStepContext publishedProviderStepContext)
        {
            _jobStepContext = jobStepContext;
            _currentSpecificationStepContext = currentSpecificationStepContext;
            _publishedFundingRepositoryStepContext = publishedFundingRepositoryStepContext;
            _policiesStepContext = policiesStepContext;
            _providersStepContext = providersStepContext;
            _publishingDatesStepContext = publishingDatesStepContext;
            _loggerStepContext = loggerStepContext;
            TemplateMapping = new TemplateMapping();
            _publishedProviderStepContext = publishedProviderStepContext;
        }

        public async Task PublishFunding(string specificationId, string jobId, string userId, string userName)
        {
            ResiliencePolicies resiliencePolicies = new ResiliencePolicies()
            {
                BlobClient = Policy.NoOpAsync(),
                CalculationsApiClient = Policy.NoOpAsync(),
                FundingFeedSearchRepository = Policy.NoOpAsync(),
                JobsApiClient = Policy.NoOpAsync(),
                PoliciesApiClient = Policy.NoOpAsync(),
                ProvidersApiClient = Policy.NoOpAsync(),
                PublishedFundingBlobRepository = Policy.NoOpAsync(),
                PublishedFundingRepository = Policy.NoOpAsync(),
                PublishedProviderVersionRepository = Policy.NoOpAsync(),
                CalculationResultsRepository = Policy.NoOpAsync(),
                SpecificationsRepositoryPolicy = Policy.NoOpAsync(),
                PublishedProviderSearchRepository = Policy.NoOpAsync(),
            };

            IVersionRepository<PublishedFundingVersion> versionRepository = new VersionRepository<PublishedFundingVersion>(_publishedFundingRepositoryStepContext.CosmosRepo);

            ILogger logger = _loggerStepContext.Logger;

            PublishedFundingIdGeneratorResolver idGeneratorResolver = new PublishedFundingIdGeneratorResolver();
            IPublishedFundingIdGenerator idGeneratorResolver10 = new Generators.Schema10.PublishedFundingIdGenerator();
            idGeneratorResolver.Register("1.0", idGeneratorResolver10);

            PublishedFundingStatusUpdateService publishedFundingStatusUpdateService = new PublishedFundingStatusUpdateService(
                _publishedFundingRepositoryStepContext.Repo,
                resiliencePolicies,
                versionRepository,
                idGeneratorResolver,
                logger, 
                new PublishingEngineOptions());

            SpecificationInMemoryRepository specificationInMemoryRepository = _currentSpecificationStepContext.Repo;

            OrganisationGroupResiliencePolicies orgResiliencePolicies = new OrganisationGroupResiliencePolicies
            {
                ProvidersApiClient = Policy.NoOpAsync(),
            };

            ProvidersInMemoryClient providersInMemoryRepository = _providersStepContext.EmulatedClient;

            OrganisationGroupTargetProviderLookup organisationGroupTargetProviderLookup = new OrganisationGroupTargetProviderLookup(providersInMemoryRepository, orgResiliencePolicies);

            OrganisationGroupGenerator organisationGroupGenerator = new OrganisationGroupGenerator(organisationGroupTargetProviderLookup);

            SpecificationFundingStatusService specificationFundingStatusService = new SpecificationFundingStatusService(logger, specificationInMemoryRepository);

            PublishPrerequisiteChecker publishPrerequisiteChecker = new PublishPrerequisiteChecker(specificationFundingStatusService, logger);

            PublishedFundingChangeDetectorService publishedFundingChangeDetectorService = new PublishedFundingChangeDetectorService();

            MapperConfiguration mapperConfiguration = new MapperConfiguration(c =>
            {
                c.AddProfile<PublishingServiceMappingProfile>();
            });

            IMapper mapper = mapperConfiguration.CreateMapper();

            PublishedFundingContentsGeneratorResolver resolver = new PublishedFundingContentsGeneratorResolver();
            IPublishedFundingContentsGenerator v10Generator = new CalculateFunding.Generators.Schema10.PublishedFundingContentsGenerator();
            resolver.Register("1.0", v10Generator);

            PublishedFundingGenerator publishedFundingGenerator = new PublishedFundingGenerator(mapper, idGeneratorResolver);

            InMemoryBlobClient inMemoryBlobClient = new InMemoryBlobClient();

            PublishedFundingInMemorySearchRepository publishedFundingInMemorySearchRepository = new PublishedFundingInMemorySearchRepository();
            PublishedProviderInMemorySearchRepository publishedProviderInMemorySearchRepository = new PublishedProviderInMemorySearchRepository();

            PublishedFundingContentsPersistanceService publishedFundingContentsPersistanceService = 
                new PublishedFundingContentsPersistanceService(resolver, _publishedFundingRepositoryStepContext.BlobRepo, resiliencePolicies, new PublishingEngineOptions());

            PublishedProviderVersionInMemoryRepository publishedProviderVersionInMemoryRepository = new PublishedProviderVersionInMemoryRepository();

            IPublishedProviderVersioningService publishedProviderVersioningService = new PublishedProviderVersioningService(logger, publishedProviderVersionInMemoryRepository, resiliencePolicies, new PublishingEngineOptions());

            IJobTracker jobTracker = new JobTracker(_jobStepContext.JobsClient, resiliencePolicies, logger);
            PublishedProviderStatusUpdateSettings publishedProviderStatusUpdateSettings = new PublishedProviderStatusUpdateSettings();

            PublishedProviderIndexerService publishedProviderIndexerService = 
                new PublishedProviderIndexerService(logger, _publishedProviderStepContext.SearchRepo, resiliencePolicies, new PublishingEngineOptions());

            IPublishedProviderStatusUpdateService publishedProviderStatusUpdateService = new PublishedProviderStatusUpdateService(
                publishedProviderVersioningService,
                _publishedFundingRepositoryStepContext.Repo,
                jobTracker,
                logger,
                publishedProviderStatusUpdateSettings
                , new PublishingEngineOptions());

            Common.ApiClient.Policies.IPoliciesApiClient policiesInMemoryRepository = _policiesStepContext.Client;

            PublishedFundingDataService publishedFundingDataService = new PublishedFundingDataService(
                _publishedFundingRepositoryStepContext.Repo,
                specificationInMemoryRepository,
                resiliencePolicies,
                new PublishingEngineOptions());
            
            IJobsApiClient jobsApiClient = new JobsInMemoryRepository();

            InMemoryAzureBlobClient inMemoryAzureBlobClient = new InMemoryAzureBlobClient();
            ICalculationResultsRepository calculationResultsRepository = new CalculationInMemoryRepository(CalculationResults);
            FundingLineTotalAggregator fundingLineTotalAggregator = new FundingLineTotalAggregator();
            PublishedProviderDataGenerator publishedProviderDataGenerator = new PublishedProviderDataGenerator(fundingLineTotalAggregator, mapper);
            PublishedProviderContentsGeneratorResolver publishedProviderContentsGeneratorResolver = new PublishedProviderContentsGeneratorResolver();
            IPublishedProviderContentsGenerator v10ProviderGenerator = new CalculateFunding.Generators.Schema10.PublishedProviderContentsGenerator();
            publishedProviderContentsGeneratorResolver.Register("1.0", v10ProviderGenerator);
            CalculationResultsService calculationResultsService = new CalculationResultsService(resiliencePolicies, calculationResultsRepository, logger, new PublishingEngineOptions());
            PublishedProviderVersionService publishedProviderVersionService = 
                new PublishedProviderVersionService(logger, _publishedProviderStepContext.BlobRepo, resiliencePolicies, jobsApiClient);

            Common.ApiClient.Calcs.ICalculationsApiClient calculationsApiClient = new CalculationsInMemoryClient(TemplateMapping);

            PublishedProviderContentPersistanceService publishedProviderContentsPersistanceService = new PublishedProviderContentPersistanceService(publishedProviderVersionService, publishedProviderIndexerService, logger, new PublishingEngineOptions());

            PublishService publishService = new PublishService(publishedFundingStatusUpdateService,
                publishedFundingDataService,
                resiliencePolicies,
                specificationInMemoryRepository,
                organisationGroupGenerator,
                publishPrerequisiteChecker,
                publishedFundingChangeDetectorService,
                publishedFundingGenerator,
                publishedProviderDataGenerator,
                publishedProviderContentsGeneratorResolver,
                publishedFundingContentsPersistanceService,
                publishedProviderContentsPersistanceService,
                _publishingDatesStepContext.Service,
                publishedProviderStatusUpdateService,
                _providersStepContext.Service,
                calculationResultsService,
                publishedFundingInMemorySearchRepository,
                publishedProviderIndexerService,
                _jobStepContext.JobsClient,
                policiesInMemoryRepository,
                calculationsApiClient,
                logger,
                new PublishingEngineOptions());

            Message message = new Message();

            message.UserProperties.Add("user-id", userId);
            message.UserProperties.Add("user-name", userName);
            message.UserProperties.Add("specification-id", specificationId);
            message.UserProperties.Add("jobId", jobId);

            await publishService.PublishResults(message);
        }

        public async Task RefreshFunding(string specificationId, string jobId, string userId, string userName)
        {
            ResiliencePolicies resiliencePolicies = new ResiliencePolicies()
            {
                BlobClient = Policy.NoOpAsync(),
                CalculationsApiClient = Policy.NoOpAsync(),
                FundingFeedSearchRepository = Policy.NoOpAsync(),
                JobsApiClient = Policy.NoOpAsync(),
                PoliciesApiClient = Policy.NoOpAsync(),
                ProvidersApiClient = Policy.NoOpAsync(),
                PublishedFundingBlobRepository = Policy.NoOpAsync(),
                PublishedFundingRepository = Policy.NoOpAsync(),
                PublishedProviderVersionRepository = Policy.NoOpAsync(),
                CalculationResultsRepository = Policy.NoOpAsync(),
                SpecificationsRepositoryPolicy = Policy.NoOpAsync(),
                PublishedProviderSearchRepository = Policy.NoOpAsync(),
            };

            PublishedFundingVersionInMemoryRepository publishedVersionInMemoryRepository = new PublishedFundingVersionInMemoryRepository();

            ILogger logger = _loggerStepContext.Logger;

            PublishedFundingIdGeneratorResolver idGeneratorResolver = new PublishedFundingIdGeneratorResolver();
            IPublishedFundingIdGenerator idGeneratorResolver10 = new Generators.Schema10.PublishedFundingIdGenerator();
            idGeneratorResolver.Register("1.0", idGeneratorResolver10);

            PublishedFundingStatusUpdateService publishedFundingStatusUpdateService = new PublishedFundingStatusUpdateService(
                _publishedFundingRepositoryStepContext.Repo,
                resiliencePolicies,
                publishedVersionInMemoryRepository,
                idGeneratorResolver,
                logger,
                new PublishingEngineOptions());

            SpecificationInMemoryRepository specificationInMemoryRepository = _currentSpecificationStepContext.Repo;

            SpecificationFundingStatusService specificationFundingStatusService = new SpecificationFundingStatusService(logger, specificationInMemoryRepository);

            CalculationEngineRunningChecker calculationEngineRunningChecker = new CalculationEngineRunningChecker(_jobStepContext.JobsClient, resiliencePolicies);

            Common.ApiClient.Calcs.ICalculationsApiClient calculationsApiClient = new CalculationsInMemoryClient(TemplateMapping, CalculationMetadata);

            CalculationPrerequisiteCheckerService calculationApprovalCheckerService = new CalculationPrerequisiteCheckerService(calculationsApiClient, resiliencePolicies, logger);

            RefreshPrerequisiteChecker refreshPrerequisiteChecker = new RefreshPrerequisiteChecker(specificationFundingStatusService, specificationInMemoryRepository, calculationEngineRunningChecker, calculationApprovalCheckerService, logger);

            MapperConfiguration mapperConfiguration = new MapperConfiguration(c =>
            {
                c.AddProfile<PublishingServiceMappingProfile>();
            });

            IMapper mapper = mapperConfiguration.CreateMapper();

            PublishedFundingContentsGeneratorResolver resolver = new PublishedFundingContentsGeneratorResolver();
            IPublishedFundingContentsGenerator v10Generator = new CalculateFunding.Generators.Schema10.PublishedFundingContentsGenerator();
            resolver.Register("1.0", v10Generator);

            PublishedProviderVersionInMemoryRepository publishedProviderVersionInMemoryRepository = new PublishedProviderVersionInMemoryRepository();

            IPublishedProviderVersioningService publishedProviderVersioningService = new PublishedProviderVersioningService(logger, publishedProviderVersionInMemoryRepository, resiliencePolicies, new PublishingEngineOptions());

            IJobTracker jobTracker = new JobTracker(_jobStepContext.JobsClient, resiliencePolicies, logger);
            PublishedProviderStatusUpdateSettings publishedProviderStatusUpdateSettings = new PublishedProviderStatusUpdateSettings();

            IPublishedProviderStatusUpdateService publishedProviderStatusUpdateService = new PublishedProviderStatusUpdateService(
                publishedProviderVersioningService,
                _publishedFundingRepositoryStepContext.Repo,
                jobTracker,
                logger,
                publishedProviderStatusUpdateSettings,
                new PublishingEngineOptions());

            ICalculationResultsRepository calculationResultsRepository = new CalculationInMemoryRepository(CalculationResults);
            FundingLineTotalAggregator fundingLineTotalAggregator = new FundingLineTotalAggregator();
            PublishedProviderDataGenerator publishedProviderDataGenerator = new PublishedProviderDataGenerator(fundingLineTotalAggregator, mapper);
            PublishedProviderContentsGeneratorResolver publishedProviderContentsGeneratorResolver = new PublishedProviderContentsGeneratorResolver();
            IPublishedProviderContentsGenerator v10ProviderGenerator = new CalculateFunding.Generators.Schema10.PublishedProviderContentsGenerator();
            publishedProviderContentsGeneratorResolver.Register("1.0", v10ProviderGenerator);
            CalculationResultsService calculationResultsService = new CalculationResultsService(resiliencePolicies, calculationResultsRepository, logger, new PublishingEngineOptions());

            PublishedProviderExclusionCheck providerExclusionCheck = new PublishedProviderExclusionCheck();

            PublishedFundingDataService publishedFundingDataService = new PublishedFundingDataService(_publishedFundingRepositoryStepContext.Repo, specificationInMemoryRepository, resiliencePolicies, new PublishingEngineOptions());

            ProfilingInMemoryClient profilingApiClient = new ProfilingInMemoryClient();

            ProfilingService profilingService = new ProfilingService(logger, profilingApiClient);

            PublishedProviderDataPopulator publishedProviderDataPopulator = new PublishedProviderDataPopulator(mapper);

            InScopePublishedProviderService inScopePublishedProviderService = new InScopePublishedProviderService(mapper, logger);

            FundingLineValueOverride fundingLineValueOverride = new FundingLineValueOverride();

            Common.ApiClient.Policies.IPoliciesApiClient policiesInMemoryRepository = _policiesStepContext.Client;

            RefreshService refreshService = new RefreshService(publishedProviderStatusUpdateService,
                publishedFundingDataService,
                resiliencePolicies,
                specificationInMemoryRepository,
                _providersStepContext.Service,
                calculationResultsService,
                publishedProviderDataGenerator,
                publishedProviderContentsGeneratorResolver,
                profilingService,
                inScopePublishedProviderService,
                publishedProviderDataPopulator,
                _jobStepContext.JobsClient,
                logger,
                calculationsApiClient,
                policiesInMemoryRepository,
                refreshPrerequisiteChecker,
                providerExclusionCheck,
                fundingLineValueOverride
            );

            Message message = new Message();

            message.UserProperties.Add("user-id", userId);
            message.UserProperties.Add("user-name", userName);
            message.UserProperties.Add("specification-id", specificationId);
            message.UserProperties.Add("jobId", jobId);

            await refreshService.RefreshResults(message);
        }

        public InMemoryPublishedFundingRepository PublishedFundingRepository { get; set; }

        public bool PublishSuccessful { get; set; }
        public bool RefreshSuccessful { get; set; }
    }
}
