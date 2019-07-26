using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Providers.Models;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Publishing.Interfaces;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace CalculateFunding.Services.Publishing.UnitTests
{
    [TestClass]
    public class RefreshServiceTests
    {
        private ISpecificationService _specificationService;
        private IProviderService _providerService;
        private IRefreshService _refreshService;

        [TestInitialize]
        public void SetUp()
        {
            _specificationService = Substitute.For<ISpecificationService>();
            _providerService = Substitute.For<IProviderService>();

            _refreshService = new RefreshService(Substitute.For<IPublishedProviderStatusUpdateService>(),
                Substitute.For<IPublishedFundingRepository>(),
                Substitute.For<IPublishingResiliencePolicies>(),
                _specificationService,
                _providerService);
        }

        [TestMethod]
        public async Task ProvidersQueryMethodDelegatesToProviderService()
        {
            RandomString providerVersionId = NewRandomString();
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
            RandomString specificationId = NewRandomString();
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