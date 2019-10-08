using System.Threading.Tasks;
using AutoMapper;
using CalculateFunding.Generators.OrganisationGroup;
using CalculateFunding.Generators.OrganisationGroup.Interfaces;
using CalculateFunding.Publishing.AcceptanceTests.Repositories;
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
        private readonly IPublishingDatesStepContext _publishingDatesStepContext;
        private readonly ILoggerStepContext _loggerStepContext;

        public PublishServiceAcceptanceStepContext(IJobStepContext jobStepContext,
            ICurrentSpecificationStepContext currentSpecificationStepContext,
            IPublishedFundingRepositoryStepContext publishedFundingRepositoryStepContext,
            IPoliciesStepContext policiesStepContext,
            IProvidersStepContext providersStepContext,
            IPublishingDatesStepContext publishingDatesStepContext,
            ILoggerStepContext loggerStepContext)
        {
            _jobStepContext = jobStepContext;
            _currentSpecificationStepContext = currentSpecificationStepContext;
            _publishedFundingRepositoryStepContext = publishedFundingRepositoryStepContext;
            _policiesStepContext = policiesStepContext;
            _providersStepContext = providersStepContext;
            _publishingDatesStepContext = publishingDatesStepContext;
            _loggerStepContext = loggerStepContext;
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
                logger);

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

            PublishedFundingContentsPersistanceService publishedFundingContentsPersistanceService = new PublishedFundingContentsPersistanceService(resolver, inMemoryBlobClient, resiliencePolicies);
            PublishedProviderVersionInMemoryRepository publishedProviderVersionInMemoryRepository = new PublishedProviderVersionInMemoryRepository();

            IPublishedProviderVersioningService publishedProviderVersioningService = new PublishedProviderVersioningService(logger, publishedProviderVersionInMemoryRepository, resiliencePolicies);

            IJobTracker jobTracker = new JobTracker(_jobStepContext.JobsClient, resiliencePolicies, logger);
            PublishedProviderStatusUpdateSettings publishedProviderStatusUpdateSettings = new PublishedProviderStatusUpdateSettings();

            PublishedProviderIndexerService publishedProviderIndexerService = new PublishedProviderIndexerService(logger, publishedProviderInMemorySearchRepository, resiliencePolicies);

            IPublishedProviderStatusUpdateService publishedProviderStatusUpdateService = new PublishedProviderStatusUpdateService(
                publishedProviderVersioningService,
                _publishedFundingRepositoryStepContext.Repo,
                jobTracker,
                logger,
                publishedProviderStatusUpdateSettings);

            Common.ApiClient.Policies.IPoliciesApiClient policiesInMemoryRepository = _policiesStepContext.Client;

            PublishedFundingDataService publishedFundingDataService = new PublishedFundingDataService(
                _publishedFundingRepositoryStepContext.Repo,
                specificationInMemoryRepository,
                resiliencePolicies);

            PublishService publishService = new PublishService(publishedFundingStatusUpdateService,
                publishedFundingDataService,
                resiliencePolicies,
                specificationInMemoryRepository,
                organisationGroupGenerator,
                publishPrerequisiteChecker,
                publishedFundingChangeDetectorService,
                publishedFundingGenerator,
                publishedFundingContentsPersistanceService,
                _publishingDatesStepContext.Service,
                publishedProviderStatusUpdateService,
                _providersStepContext.Service,
                publishedFundingInMemorySearchRepository,
                publishedProviderIndexerService,
                _jobStepContext.JobsClient,
                policiesInMemoryRepository,
                logger
                );

            Message message = new Message();

            message.UserProperties.Add("user-id", userId);
            message.UserProperties.Add("user-name", userName);
            message.UserProperties.Add("specification-id", specificationId);
            message.UserProperties.Add("jobId", jobId);

            await publishService.PublishResults(message);
        }

        public InMemoryPublishedFundingRepository PublishedFundingRepository { get; set; }

        public bool PublishSuccessful { get; set; }
    }
}
