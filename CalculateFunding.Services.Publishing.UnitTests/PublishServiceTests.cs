using CalculateFunding.Common.ApiClient.Calcs;
using CalculateFunding.Common.ApiClient.Jobs;
using CalculateFunding.Common.ApiClient.Policies;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Generators.OrganisationGroup;
using CalculateFunding.Generators.OrganisationGroup.Interfaces;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Publishing.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Polly;
using Serilog;

namespace CalculateFunding.Services.Publishing.UnitTests
{
    [TestClass]
    public class PublishServiceTests
    {
        private IPublishService _publishService;

        private IPublishedFundingStatusUpdateService _publishedFundingStatusUpdateService;
        private IPublishedFundingDataService _publishedFundingDataService;
        private ISpecificationService _specificationService;

        // TODO: Change to IOrganisationGroupGenerator once common is updated
        private OrganisationGroupGenerator _organisationGroupGenerator;
        private IJobsApiClient _jobsApiClient;
        private ILogger _logger;
        private IPoliciesApiClient _policiesApiClient;
        private IProviderService _providerService;

        private IPublishPrerequisiteChecker _publishPrerequisiteChecker;
        private IPublishedFundingChangeDetectorService _publishedFundingChangeDetectorService;
        private IPublishedFundingGenerator _publishedFundingGenerator;
        private IPublishedFundingContentsPersistanceService _publishedFundingContentsPersistanceService;
        private IPublishedProviderContentPersistanceService _publishedProviderContentPersistanceService;
        private IPublishedFundingDateService _publishedFundingDateService;
        private IPublishedProviderStatusUpdateService _publishedProviderStatusUpdateService;
        private IPublishingResiliencePolicies _resiliencePolicies;
        private IPublishedProviderDataGenerator _publishedProviderDataGenerator;
        private IPublishedProviderContentsGeneratorResolver _publishedProviderContentsGeneratorResolver;
        private ICalculationResultsService _calculationResultsService;
        private IPublishedProviderVersionService _publishedProviderVersionService;
        private ICalculationsApiClient _calculationsApiClient;
        private IConfiguration _configuration;

        ISearchRepository<PublishedFundingIndex> _publishedFundingSearchRepository;
        private IPublishedProviderIndexerService _publishedProviderIndexerService;
        private IJobManagement _jobManagement;

        [TestInitialize]
        public void SetUp()
        {
            _publishedFundingStatusUpdateService = Substitute.For<IPublishedFundingStatusUpdateService>();
            _publishedFundingDataService = Substitute.For<IPublishedFundingDataService>();
            _specificationService = Substitute.For<ISpecificationService>();
            _organisationGroupGenerator = new OrganisationGroupGenerator(Substitute.For<IOrganisationGroupTargetProviderLookup>());
            _publishPrerequisiteChecker = Substitute.For<IPublishPrerequisiteChecker>();
            _publishedFundingChangeDetectorService = Substitute.For<IPublishedFundingChangeDetectorService>();
            _publishedFundingGenerator = Substitute.For<IPublishedFundingGenerator>();
            _publishedFundingContentsPersistanceService = Substitute.For<IPublishedFundingContentsPersistanceService>();
            _publishedFundingDateService = Substitute.For<IPublishedFundingDateService>();
            _publishedProviderStatusUpdateService = Substitute.For<IPublishedProviderStatusUpdateService>();
            _providerService = Substitute.For<IProviderService>();
            _publishedFundingSearchRepository = Substitute.For<ISearchRepository<PublishedFundingIndex>>();
            _publishedProviderIndexerService = Substitute.For<IPublishedProviderIndexerService>();
            _jobsApiClient = Substitute.For<IJobsApiClient>();
            _policiesApiClient = Substitute.For<IPoliciesApiClient>();
            _logger = Substitute.For<ILogger>();
            _publishedProviderContentPersistanceService = Substitute.For<IPublishedProviderContentPersistanceService>();

            _publishedProviderDataGenerator = Substitute.For<IPublishedProviderDataGenerator>();
            _publishedProviderContentsGeneratorResolver = Substitute.For<IPublishedProviderContentsGeneratorResolver>();
            _calculationResultsService = Substitute.For<ICalculationResultsService>();
            _publishedProviderVersionService = Substitute.For<IPublishedProviderVersionService>();
            _calculationsApiClient = Substitute.For<ICalculationsApiClient>();
            _configuration = Substitute.For<IConfiguration>();
            _jobManagement = Substitute.For<IJobManagement>();

            _resiliencePolicies = new ResiliencePolicies()
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

            _publishService = new PublishService(_publishedFundingStatusUpdateService,
                _publishedFundingDataService,
                _resiliencePolicies,
                _specificationService,
                _organisationGroupGenerator,
                _publishPrerequisiteChecker,
                _publishedFundingChangeDetectorService,
                _publishedFundingGenerator,
                _publishedProviderDataGenerator,
                _publishedProviderContentsGeneratorResolver,
                _publishedFundingContentsPersistanceService,
                _publishedProviderContentPersistanceService,
                _publishedFundingDateService,
                _publishedProviderStatusUpdateService,
                _providerService,
                _publishedFundingSearchRepository,
                _publishedProviderIndexerService,
                _jobsApiClient,
                _policiesApiClient,
                _calculationsApiClient,
                _logger,
                new PublishingEngineOptions(_configuration),
                _jobManagement);
        }
    }
}