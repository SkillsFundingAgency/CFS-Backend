using System.Threading.Tasks;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Publishing.Interfaces;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using ApiSpecificationSummary = CalculateFunding.Common.ApiClient.Specifications.Models.SpecificationSummary;

namespace CalculateFunding.Services.Publishing.UnitTests
{
    [TestClass]
    public class PublishServiceTests
    {
        private ISpecificationService _specificationService;
        private IPublishService _publishService;

        [TestInitialize]
        public void SetUp()
        {
            _specificationService = Substitute.For<ISpecificationService>();

            _publishService = new PublishService(Substitute.For<IPublishedProviderStatusUpdateService>(),
                Substitute.For<IPublishedFundingRepository>(),
                Substitute.For<IPublishingResiliencePolicies>(),
                _specificationService);
        }

        [TestMethod]
        public async Task SpecificationQueryMethodDelegatesToSpecificationService()
        {
            RandomString specificationId = new RandomString();
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