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
                ResultsRepository = Policy.NoOpAsync(),
                SpecificationsRepositoryPolicy = Policy.NoOpAsync(),
            };

            InMemoryPublishedFundingRepository publishFundingRepository = _publishedFundingRepositoryStepContext.Repo;
            PublishedFundingRepository = publishFundingRepository;

            PublishedVersionInMemoryRepository publishedVersionInMemoryRepository = new PublishedVersionInMemoryRepository();

            ILogger logger = _loggerStepContext.Logger;

            PublishedFundingStatusUpdateService publishedFundingStatusUpdateService = new PublishedFundingStatusUpdateService(publishFundingRepository, resiliencePolicies, publishedVersionInMemoryRepository, logger);

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


            PublishedFundingIdGeneratorResolver idGeneratorResolver = new PublishedFundingIdGeneratorResolver();
            IPublishedFundingIdGenerator idGeneratorResolver10 = new Generators.Schema10.PublishedFundingIdGenerator();
            idGeneratorResolver.Register("1.0", idGeneratorResolver10);



            PublishedFundingGenerator publishedFundingGenerator = new PublishedFundingGenerator(mapper, idGeneratorResolver);

            InMemoryBlobClient inMemoryBlobClient = new InMemoryBlobClient();

            PublishedFundingInMemorySearchRepository publishedFundingInMemorySearchRepository = new PublishedFundingInMemorySearchRepository();

            PublishedFundingContentsPersistanceService publishedFundingContentsPersistanceService = new PublishedFundingContentsPersistanceService(resolver, inMemoryBlobClient, resiliencePolicies, publishedFundingInMemorySearchRepository);

            Common.ApiClient.Policies.IPoliciesApiClient policiesInMemoryRepository = _policiesStepContext.Client;

            PublishService publishService = new PublishService(publishedFundingStatusUpdateService,
                publishFundingRepository,
                resiliencePolicies,
                specificationInMemoryRepository,
                organisationGroupGenerator,
                publishPrerequisiteChecker,
                publishedFundingChangeDetectorService,
                publishedFundingGenerator,
                publishedFundingContentsPersistanceService,
                _publishingDatesStepContext.Service,
                _providersStepContext.Service,
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
