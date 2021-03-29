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
        private Mock<IPublishedFundingBulkRepository> _publishedFundingBulk;
        private PublishedFundingDataService _dataService;

        [TestInitialize]
        public void SetUp()
        {
            _publishedFunding = new Mock<IPublishedFundingRepository>();
            _publishedFundingBulk = new Mock<IPublishedFundingBulkRepository>();

            _dataService = new PublishedFundingDataService(_publishedFunding.Object,
                new Mock<ISpecificationService>().Object,
                new ResiliencePolicies
                {
                    SpecificationsRepositoryPolicy = Policy.NoOpAsync(),
                    PublishedFundingRepository = Policy.NoOpAsync()
                },
                _publishedFundingBulk.Object);
        }

        [TestMethod]
        public async Task GetPublishedProviderFundingLinesAsksForFundingLineNamesForPaymentTypeFundingLines()
        {
            string specificationId = new RandomString();
            (string Code, string Name)[] expectedFundingLineNames =
            {
                (NewRandomString(), NewRandomString()),
                (NewRandomString(), NewRandomString()),
                (NewRandomString(), NewRandomString())
            };

            GivenThePaymentFundingLines(specificationId, expectedFundingLineNames);

            IEnumerable<(string Code, string Name)> actualFundingLines = await _dataService.GetPublishedProviderFundingLines(specificationId);

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
            Func<Task<IEnumerable<(string Code, string Name)>>> invocation = () => _dataService.GetPublishedProviderFundingLines(specificationId);

            invocation
                .Should()
                .Throw<ArgumentNullException>()
                .Which
                .ParamName
                .Should()
                .Be("specificationId");
        }

        [TestMethod]
        public async Task DeletePublishedProvidersCallsUnderlyingDeletePublishedProvidersRepoMethod()
        {
            IEnumerable<PublishedProvider> publishedProviders = new List<PublishedProvider>
            {
                new PublishedProvider(),
                new PublishedProvider()
            };

            GivenTheDeletePublishedProviders(publishedProviders);

            await _dataService.DeletePublishedProviders(publishedProviders);

            _publishedFunding.Verify();
        }

        [TestMethod]
        public void DeletePublishedProvidersGuardsAgainstEmptyPublishedProvidersSupplied()
        {
            Func<Task> invocation = () => _dataService.DeletePublishedProviders(Enumerable.Empty<PublishedProvider>());

            invocation
                .Should()
                .Throw<ArgumentNullException>()
                .Which
                .ParamName
                .Should()
                .Be("publishedProviders");
        }

        [TestMethod]
        public async Task GetCurrentPublishedFundingsWhenRequestedBySpecificationId()
        {
            const string specificationId = "spec-1";

            IEnumerable<KeyValuePair<string, string>> publishedFundingIds = new[] {
                    new KeyValuePair<string, string>("pf1", "p1"),
                    new KeyValuePair<string, string>("pf2", "p2")
                };

            _publishedFunding.Setup(x => x.GetPublishedFundingIds(specificationId, GroupingReason.Payment))
                .ReturnsAsync(publishedFundingIds)
                .Verifiable();

            IEnumerable<PublishedFunding> expectedPublishedFundings = new List<PublishedFunding>
            {
                new PublishedFunding(),
                new PublishedFunding()
            };

            _publishedFundingBulk.Setup(x => x.GetPublishedFundings(publishedFundingIds))
                .ReturnsAsync(() => expectedPublishedFundings);

            IEnumerable<PublishedFunding> publishedFundings = await _dataService.GetCurrentPublishedFunding(specificationId, GroupingReason.Payment);

            publishedFundings.Count().Should().Be(2);
        }

        private void GivenThePaymentFundingLines(string specificationId,
            IEnumerable<(string Code, string Name)> fundingLines)
        {
            _publishedFunding.Setup(_ => _.GetPublishedProviderFundingLines(specificationId, GroupingReason.Payment))
                .ReturnsAsync(fundingLines)
                .Verifiable();
        }

        private void GivenTheDeletePublishedProviders(IEnumerable<PublishedProvider> publishedProviders)
        {
            _publishedFunding.Setup(_ => _.DeletePublishedProviders(publishedProviders))
                .Verifiable();
        }

        private string NewRandomString()
        {
            return new RandomString();
        }
    }
}