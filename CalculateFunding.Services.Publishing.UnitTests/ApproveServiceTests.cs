using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Publishing.Interfaces;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace CalculateFunding.Services.Publishing.UnitTests
{
    [TestClass]
    public class ApproveServiceTests
    {
        private ISpecificationService _specificationService;
        private IApproveService _approveService;

        [TestInitialize]
        public void SetUp()
        {
            _specificationService = Substitute.For<ISpecificationService>();

            _approveService = new ApproveService(Substitute.For<IPublishedProviderStatusUpdateService>(),
                Substitute.For<IPublishedFundingRepository>(),
                Substitute.For<IPublishingResiliencePolicies>(),
                _specificationService);
        }

        [TestMethod]
        public async Task SpecificationQueryMethodDelegatesToSpecificationService()
        {
            RandomString specificationId = new RandomString();
            SpecificationSummary expectedSpecificationSummary = new SpecificationSummary();

            GivenTheSpecificationSummaryForId(specificationId, expectedSpecificationSummary);

            SpecificationSummary response = await _approveService.GetSpecificationSummaryById(specificationId);

            response
                .Should()
                .BeSameAs(expectedSpecificationSummary);
        }

        private void GivenTheSpecificationSummaryForId(string specificationId, SpecificationSummary specificationSummary)
        {
            _specificationService.GetSpecificationSummaryById(specificationId)
                .Returns(specificationSummary);
        }
    }
}