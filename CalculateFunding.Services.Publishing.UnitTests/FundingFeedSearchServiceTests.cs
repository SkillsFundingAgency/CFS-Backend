using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Repositories.Common.Search;
using Microsoft.Azure.Search.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Polly;

namespace CalculateFunding.Services.Publishing.UnitTests
{
    [TestClass]
    public class FundingFeedSearchServiceTests
    {
        private FundingFeedSearchService _searchService;
        private ISearchRepository<PublishedFundingIndex> _searchRepository;

        [TestInitialize]
        public void SetUp()
        {
            _searchRepository = Substitute.For<ISearchRepository<PublishedFundingIndex>>();
            
            _searchService = new FundingFeedSearchService(_searchRepository, new ResiliencePolicies
            {
                FundingFeedSearchRepository = Policy.NoOpAsync(),
            });
        }
        
        [TestMethod]
        public async Task SortsByBothStatusUpdateDateAndDocumentIdForAStableSortWhenPageRefSupplied()
        {
            await _searchService.GetFeedsV3(1, 10);

            await _searchRepository
                .Received(1)
                .Search(Arg.Is(""),
                    Arg.Is<SearchParameters>(_ =>
                        _.OrderBy.SequenceEqual(new[] { "statusChangedDate asc", "id asc" })));
        }

        [TestMethod]
        public async Task SortsByBothStatusUpdateDateAndDocumentIdForAStableSortWhenPageRefNotSupplied()
        {
            await _searchService.GetFeedsV3(null, 10);

            await _searchRepository
                .Received(1)
                .Search(Arg.Is(""),
                    Arg.Is<SearchParameters>(_ =>
                        _.OrderBy.SequenceEqual(new[] { "statusChangedDate desc", "id asc" })));
        }
    }
}