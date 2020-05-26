using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Polly;

namespace CalculateFunding.Services.Publishing.UnitTests
{
    [TestClass]
    public class PublishedFundingDataServiceTests
    {
        private Mock<IPublishedFundingRepository> _publishedFunding;
        private PublishedFundingDataService _dataService;

        [TestInitialize]
        public void SetUp()
        {
            _publishedFunding = new Mock<IPublishedFundingRepository>();

            _dataService = new PublishedFundingDataService(_publishedFunding.Object,
                new Mock<ISpecificationService>().Object,
                new ResiliencePolicies
                {
                    SpecificationsRepositoryPolicy = Policy.NoOpAsync(),
                    PublishedFundingRepository = Policy.NoOpAsync()
                },
                new Mock<IPublishingEngineOptions>().Object,
                new Mock<IPoliciesService>().Object);
        }

        [TestMethod]
        public async Task GetPublishedProviderFundingLinesAsksForFundingLineNamesForPaymentTypeFundingLines()
        {
            string specificationId = new RandomString();
            string[] expectedFundingLineNames =
            {
                NewRandomString(),
                NewRandomString(),
                NewRandomString()
            };

            GivenThePaymentFundingLines(specificationId, expectedFundingLineNames);

            IEnumerable<string> actualFundingLines = await _dataService.GetPublishedProviderFundingLines(specificationId);

            actualFundingLines
                .Should()
                .BeEquivalentTo(expectedFundingLineNames);

            _publishedFunding.Verify();
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        public void GetPublishedProviderFundingLinesGuardsAgainstMissingSpecificationIdSupplied(string specificationId)
        {
            Func<Task<IEnumerable<string>>> invocation = () => _dataService.GetPublishedProviderFundingLines(specificationId);

            invocation
                .Should()
                .Throw<ArgumentNullException>()
                .Which
                .ParamName
                .Should()
                .Be("specificationId");
        }

        private void GivenThePaymentFundingLines(string specificationId,
            IEnumerable<string> fundingLines)
        {
            _publishedFunding.Setup(_ => _.GetPublishedProviderFundingLines(specificationId, GroupingReason.Payment))
                .ReturnsAsync(fundingLines)
                .Verifiable();
        }

        private string NewRandomString()
        {
            return new RandomString();
        }
    }
}