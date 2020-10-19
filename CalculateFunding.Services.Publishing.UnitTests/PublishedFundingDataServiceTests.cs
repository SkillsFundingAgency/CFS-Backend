using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Models.Publishing;
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
        private Mock<IPublishingEngineOptions> _publishingEngineOptions;

        [TestInitialize]
        public void SetUp()
        {
            _publishedFunding = new Mock<IPublishedFundingRepository>();
            _publishingEngineOptions = new Mock<IPublishingEngineOptions>();

            _dataService = new PublishedFundingDataService(_publishedFunding.Object,
                new Mock<ISpecificationService>().Object,
                new ResiliencePolicies
                {
                    SpecificationsRepositoryPolicy = Policy.NoOpAsync(),
                    PublishedFundingRepository = Policy.NoOpAsync()
                },
                _publishingEngineOptions.Object,
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
        
        [TestMethod]
        public async Task GetCurrentPublishedFundingsWhenRequestedBySpecificationId()
        {
            const string specificationId = "spec-1";

            _publishedFunding.Setup(x => x.GetPublishedFundingIds(specificationId, GroupingReason.Payment))
                .ReturnsAsync(new[] {
                    new KeyValuePair<string, string>("pf1", "p1"),
                    new KeyValuePair<string, string>("pf2", "p2")
                })
                .Verifiable();

            _publishedFunding.Setup(x => x.GetPublishedFundingById(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(() => new PublishedFunding());
            _publishingEngineOptions.SetupGet(x => x.GetCurrentPublishedFundingConcurrencyCount)
                .Returns(2);

            IEnumerable<PublishedFunding> publishedFundings = await _dataService.GetCurrentPublishedFunding(specificationId, GroupingReason.Payment);

            publishedFundings.Count().Should().Be(2);
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