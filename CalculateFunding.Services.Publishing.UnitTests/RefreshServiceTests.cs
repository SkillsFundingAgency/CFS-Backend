using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Jobs;
using CalculateFunding.Common.ApiClient.Providers.Models;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Serilog;

namespace CalculateFunding.Services.Publishing.UnitTests
{
    [TestClass]
    public class RefreshServiceTests
    {
        private ISpecificationService _specificationService;
        private IProviderService _providerService;
        private IRefreshService _refreshService;

        private IPublishedProviderStatusUpdateService _publishedProviderStatusUpdateService;
        private IPublishedFundingRepository _publishedFundingRepository;
        private ICalculationResultsRepository _calculationResultsRepository;
        private IFundingLineGenerator _fundingLineGenerator;
        private IPublishedProviderContentsGeneratorResolver _publishedProviderContentsGeneratorResolver;
        private IJobsApiClient _jobsApiClient;
        private IProfilingService _profilingService;
        private IInScopePublishedProviderService _inScopePublishedProviderService;
        private IPublishedProviderDataPopulator _publishedProviderDataPopulator;
        private ILogger _logger;
        private ISpecificationFundingStatusService _specificationFundingStatusService;

        [TestInitialize]
        public void SetUp()
        {
            _specificationService = Substitute.For<ISpecificationService>();
            _providerService = Substitute.For<IProviderService>();
            _calculationResultsRepository = Substitute.For<ICalculationResultsRepository>();
            _fundingLineGenerator = Substitute.For<IFundingLineGenerator>();
            _publishedProviderContentsGeneratorResolver = Substitute.For<IPublishedProviderContentsGeneratorResolver>();
            _inScopePublishedProviderService = Substitute.For<IInScopePublishedProviderService>();
            _publishedProviderDataPopulator = Substitute.For<IPublishedProviderDataPopulator>();
            _jobsApiClient = Substitute.For<IJobsApiClient>();
            _profilingService = Substitute.For<IProfilingService>();
            _logger = Substitute.For<ILogger>();
            _specificationFundingStatusService = Substitute.For<ISpecificationFundingStatusService>();

            _refreshService = new RefreshService(Substitute.For<IPublishedProviderStatusUpdateService>(),
                Substitute.For<IPublishedFundingRepository>(),
                Substitute.For<IPublishingResiliencePolicies>(),
                _specificationService,
                _providerService,
                _calculationResultsRepository,
                _fundingLineGenerator,
                _publishedProviderContentsGeneratorResolver,
                _profilingService,
                _inScopePublishedProviderService,
                _publishedProviderDataPopulator,
                _jobsApiClient,
                _logger,
                _specificationFundingStatusService);
        }

        [TestMethod]
        public async Task ProvidersQueryMethodDelegatesToProviderService()
        {
            string providerVersionId = NewRandomString();
            IEnumerable<Provider> expectedProviders = new Provider[0];

            GivenTheProvidersForProviderVersionId(providerVersionId, expectedProviders);

            IEnumerable<Provider> response = await _refreshService.GetProvidersByProviderVersionId(providerVersionId);

            response
                .Should()
                .BeSameAs(expectedProviders);
        }

        [TestMethod]
        public async Task SpecificationQueryMethodDelegatesToSpecificationService()
        {
            string specificationId = NewRandomString();
            SpecificationSummary expectedSpecificationSummary = new SpecificationSummary();

            GivenTheSpecificationSummaryForId(specificationId, expectedSpecificationSummary);

            SpecificationSummary response = await _refreshService.GetSpecificationSummaryById(specificationId);

            response
                .Should()
                .BeSameAs(expectedSpecificationSummary);
        }

        private void GivenTheSpecificationSummaryForId(string specificationId, SpecificationSummary specificationSummary)
        {
            _specificationService.GetSpecificationSummaryById(specificationId)
                .Returns(specificationSummary);
        }

        private void GivenTheProvidersForProviderVersionId(string providerVersionId, IEnumerable<Provider> providers)
        {
            _providerService.GetProvidersByProviderVersionsId(providerVersionId)
                .Returns(providers);
        }

        private string NewRandomString()
        {
            return new RandomString();
        }
    }
}