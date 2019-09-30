using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Jobs;
using CalculateFunding.Common.ApiClient.Policies;
using CalculateFunding.Generators.OrganisationGroup;
using CalculateFunding.Generators.OrganisationGroup.Interfaces;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Polly;
using Serilog;
using ApiSpecificationSummary = CalculateFunding.Common.ApiClient.Specifications.Models.SpecificationSummary;

namespace CalculateFunding.Services.Publishing.UnitTests
{
    [TestClass]
    public class PublishServiceTests
    {
        private IPublishService _publishService;

        private IPublishedFundingStatusUpdateService _publishedFundingStatusUpdateService;
        private IPublishedFundingRepository _publishedFundingRepository;
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
        private IPublishedFundingDateService _publishedFundingDateService;
        private IPublishingResiliencePolicies _resiliencePolicies;

        [TestInitialize]
        public void SetUp()
        {
            _publishedFundingStatusUpdateService = Substitute.For<IPublishedFundingStatusUpdateService>();
            _publishedFundingRepository = Substitute.For<IPublishedFundingRepository>();
            _specificationService = Substitute.For<ISpecificationService>();
            _organisationGroupGenerator = new OrganisationGroupGenerator(Substitute.For<IOrganisationGroupTargetProviderLookup>());
            _publishPrerequisiteChecker = Substitute.For<IPublishPrerequisiteChecker>();
            _publishedFundingChangeDetectorService = Substitute.For<IPublishedFundingChangeDetectorService>();
            _publishedFundingGenerator = Substitute.For<IPublishedFundingGenerator>();
            _publishedFundingContentsPersistanceService = Substitute.For<IPublishedFundingContentsPersistanceService>();
            _publishedFundingDateService = Substitute.For<IPublishedFundingDateService>();
            _providerService = Substitute.For<IProviderService>();
            _jobsApiClient = Substitute.For<IJobsApiClient>();
            _policiesApiClient = Substitute.For<IPoliciesApiClient>();
            _logger = Substitute.For<ILogger>();


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
                ResultsRepository = Policy.NoOpAsync(),
                SpecificationsRepositoryPolicy = Policy.NoOpAsync(),
            };

            _publishService = new PublishService(_publishedFundingStatusUpdateService,
                _publishedFundingRepository,
                _resiliencePolicies,
                _specificationService,
                _organisationGroupGenerator,
                _publishPrerequisiteChecker,
                _publishedFundingChangeDetectorService,
                _publishedFundingGenerator,
                _publishedFundingContentsPersistanceService,
                _publishedFundingDateService,
                _providerService,
                _jobsApiClient,
                _policiesApiClient,
                _logger);
        }

        [TestMethod]
        public async Task SpecificationQueryMethodDelegatesToSpecificationService()
        {
            string specificationId = new RandomString();
            ApiSpecificationSummary expectedSpecificationSummary = new ApiSpecificationSummary();

            GivenTheSpecificationSummaryForId(specificationId, expectedSpecificationSummary);

            ApiSpecificationSummary response = await _publishService.GetSpecificationSummaryById(specificationId);

            response
                .Should()
                .BeSameAs(expectedSpecificationSummary);
        }

        private void GivenTheSpecificationSummaryForId(string specificationId, ApiSpecificationSummary specificationSummary)
        {
            _specificationService.GetSpecificationSummaryById(specificationId)
                .Returns(specificationSummary);
        }
    }
}