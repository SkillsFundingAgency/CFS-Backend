using CalculateFunding.Models.External;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Polly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.UnitTests
{
    [TestClass]
    public class FundingFeedSearchServiceTests
    {
        private FundingFeedSearchService _searchService;
        private Mock<IPublishedFundingRepository> _publishedFundingRepository;
        private IEnumerable<string> _fundingStreamIds;
        private IEnumerable<string> _fundingPeriodIds;
        private IEnumerable<string> _groupingReasons;
        private IEnumerable<string> _variationReasons;

        [TestInitialize]
        public void SetUp()
        {
            _publishedFundingRepository = new Mock<IPublishedFundingRepository>();

            _searchService = new FundingFeedSearchService(_publishedFundingRepository.Object,
                new ResiliencePolicies
                {
                    PublishedFundingRepository = Policy.NoOpAsync(),
                });

            _fundingStreamIds = NewRandomStrings();
            _fundingPeriodIds = NewRandomStrings();
            _groupingReasons = NewRandomStrings();
            _variationReasons = NewRandomStrings();
        }

        [TestMethod]
        public void GuardsAgainstPageRefLessThan1()
        {
            Func<Task<SearchFeedResult<PublishedFundingIndex>>> invocation = () => WhenTheFeedIsRequested(0, 10);

            invocation
                .Should()
                .Throw<ArgumentException>()
                .Which
                .ParamName
                .Should()
                .Be("pageRef");
        }

        [TestMethod]
        public async Task RetrievesAndReversesTheLastPageIfNoPageRefSupplied()
        {
            int totalCount = 257;
            int top = 50;

            IEnumerable<PublishedFundingIndex> sourceResults = NewResults(
                NewPublishedFundingIndex(),
                NewPublishedFundingIndex(),
                NewPublishedFundingIndex());

            GivenTheTotalCount(totalCount);
            AndTheFundingFeedResults(top, null, totalCount, sourceResults);

            SearchFeedResult<PublishedFundingIndex> fundingFeedResults = await WhenTheFeedIsRequested(null, top);

            fundingFeedResults
                .Should()
                .NotBeNull();

            fundingFeedResults
                .Top
                .Should()
                .Be(top);

            fundingFeedResults
                .TotalCount
                .Should()
                .Be(totalCount);

            fundingFeedResults
                .Entries
                .Should()
                .BeEquivalentTo(sourceResults.Reverse(),
                    opt => opt.WithStrictOrdering());
        }

        [TestMethod]
        public async Task RetrievesRequestPageIfPageRefSupplied()
        {
            int totalCount = 257;
            int top = 50;
            int pageRef = 2;

            IEnumerable<PublishedFundingIndex> sourceResults = NewResults(
                NewPublishedFundingIndex(),
                NewPublishedFundingIndex(),
                NewPublishedFundingIndex());

            GivenTheTotalCount(totalCount);
            AndTheFundingFeedResults(top, pageRef, totalCount, sourceResults);

            SearchFeedResult<PublishedFundingIndex> fundingFeedResults = await WhenTheFeedIsRequested(pageRef, top);

            fundingFeedResults
                .Should()
                .NotBeNull();

            fundingFeedResults
                .PageRef
                .Should()
                .Be(pageRef);//we only set this if they ask for page

            fundingFeedResults
                .Top
                .Should()
                .Be(top);

            fundingFeedResults
                .TotalCount
                .Should()
                .Be(totalCount);

            fundingFeedResults
                .Entries
                .Should()
                .BeEquivalentTo(sourceResults,
                    opt => opt.WithStrictOrdering());
        }

        private void GivenTheTotalCount(int totalCount)
        {
            _publishedFundingRepository.Setup(_ => _.QueryPublishedFundingCount(_fundingStreamIds,
                    _fundingPeriodIds,
                    _groupingReasons,
                    _variationReasons))
                .ReturnsAsync(totalCount);
        }

        private void AndTheFundingFeedResults(
            int top,
            int? pageRef,
            int totalCount,
            IEnumerable<PublishedFundingIndex> fundingFeedResults)
        {
            _publishedFundingRepository.Setup(_ => _.QueryPublishedFunding(_fundingStreamIds,
                    _fundingPeriodIds,
                    _groupingReasons,
                    _variationReasons,
                    top,
                    pageRef,
                    totalCount))
                .ReturnsAsync(fundingFeedResults);
        }

        private async Task<SearchFeedResult<PublishedFundingIndex>> WhenTheFeedIsRequested(int? pageRef,
            int top)
        {
            return await _searchService.GetFeedsV3(pageRef,
                top,
                _fundingStreamIds,
                _fundingPeriodIds,
                _groupingReasons,
                _variationReasons);
        }

        private IEnumerable<string> NewRandomStrings()
        {
            int count = new RandomNumberBetween(1, 3);

            for (int id = 0; id < count; id++)
            {
                yield return NewRandomString();
            }
        }

        private string NewRandomString() => new RandomString();

        private PublishedFundingIndex NewPublishedFundingIndex() => new PublishedFundingIndexBuilder()
            .Build();

        private IEnumerable<PublishedFundingIndex> NewResults(params PublishedFundingIndex[] results) => results;
    }
}