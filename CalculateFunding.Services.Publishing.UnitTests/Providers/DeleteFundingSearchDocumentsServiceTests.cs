using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Publishing.Providers;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.Azure.Search.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Polly;
using Serilog;

namespace CalculateFunding.Services.Publishing.UnitTests.Providers
{
    [TestClass]
    public class DeleteFundingSearchDocumentsServiceTests
    {
        private ISearchRepository<PublishedProviderIndex> _publishedProviderSearch;
        private ISearchRepository<PublishedFundingIndex> _publishedFundingSearch;

        private DeleteFundingSearchDocumentsService _service;

        private string _fundingPeriodId;
        private string _fundingStreamId;

        [TestInitialize]
        public void SetUp()
        {
            _publishedFundingSearch = Substitute.For<ISearchRepository<PublishedFundingIndex>>();
            _publishedProviderSearch = Substitute.For<ISearchRepository<PublishedProviderIndex>>();

            _service = new DeleteFundingSearchDocumentsService(_publishedProviderSearch,
                _publishedFundingSearch,
                new ResiliencePolicies
                {
                    PublishedProviderSearchRepository = Policy.NoOpAsync()
                },
                Substitute.For<ILogger>());

            _fundingPeriodId = NewRandomString();
            _fundingStreamId = NewRandomString();
        }

        [TestMethod]
        public void ThrowsExceptionForUnsupportedSearchIndexes()
        {
            Func<Task> invocation = WhenTheFundingSearchDocumentsAreDeleted<object>;

            invocation
                .Should()
                .ThrowAsync<ArgumentOutOfRangeException>()
                .Result
                .Which
                .ParamName
                .Should()
                .Be("SearchIndexType");
        }

        [TestMethod]
        public async Task DeletesPublishedProviderIndexDocumentsForFundingStreamAndPeriod()
        {
            IEnumerable<PublishedProviderIndex> pageOne = new[]
            {
                NewPublishedProviderIndex(),
                NewPublishedProviderIndex(),
                NewPublishedProviderIndex(),
                NewPublishedProviderIndex(),
                NewPublishedProviderIndex()
            };
            
            IEnumerable<PublishedProviderIndex> pageTwo = new[]
            {
                NewPublishedProviderIndex(),
                NewPublishedProviderIndex(),
                NewPublishedProviderIndex()
            };
            
            GivenThePagesOfPublishedProviderIndexResults(pageOne, pageTwo);

            await WhenTheFundingSearchDocumentsAreDeleted<PublishedProviderIndex>();

            await ThenTheMatchingPublishedProviderDocumentsAreDeleted(pageOne);
            await AndTheMatchingPublishedProviderDocumentsAreDeleted(pageTwo);
        }
        
        [TestMethod]
        public async Task DeletesPublishedFundingIndexDocumentsForFundingStreamAndPeriod()
        {
            IEnumerable<PublishedFundingIndex> pageOne = new[]
            {
                NewPublishedFundingIndex(),
                NewPublishedFundingIndex(),
                NewPublishedFundingIndex()
            };
            
            IEnumerable<PublishedFundingIndex> pageTwo = new[]
            {
                NewPublishedFundingIndex(),
                NewPublishedFundingIndex(),
                NewPublishedFundingIndex()
            };
            
            GivenThePagesOfPublishedFundingIndexResults(pageOne, pageTwo);

            await WhenTheFundingSearchDocumentsAreDeleted<PublishedFundingIndex>();

            await ThenTheMatchingPublishedFundingDocumentsAreDeleted(pageOne);
            await AndTheMatchingPublishedFundingDocumentsAreDeleted(pageTwo);
        }

        private void GivenThePagesOfPublishedProviderIndexResults(params IEnumerable<PublishedProviderIndex>[] pages)
        {
            GivenThePagesOfIndexResults(_publishedProviderSearch, pages);
        }
        
        private void GivenThePagesOfPublishedFundingIndexResults(params IEnumerable<PublishedFundingIndex>[] pages)
        {
            GivenThePagesOfIndexResults(_publishedFundingSearch, pages);
        }
        
        private void GivenThePagesOfIndexResults<TIndex>(ISearchRepository<TIndex> searchRepository,
            params IEnumerable<TIndex>[] pages)
        where TIndex : class
        {
            int pageNumber = 0;
            
            foreach (IEnumerable<TIndex> page in pages)
            {
                int number = pageNumber;

                searchRepository.Search(Arg.Is(string.Empty),
                        Arg.Is<SearchParameters>(_ => _.Top == 50 &&
                                                      _.SearchMode == SearchMode.Any &&
                                                      _.Filter.Equals($"fundingStreamId eq '{_fundingStreamId}' and fundingPeriodId eq '{_fundingPeriodId}'") &&
                                                      _.QueryType == QueryType.Full &&
                                                      _.Skip == number * 50),
                        Arg.Is(false))
                    .Returns(new SearchResults<TIndex>
                    {
                        Results = page.Select(_ => new CalculateFunding.Repositories.Common.Search.SearchResult<TIndex>
                        {
                            Result = _
                        }).ToList()
                    });

                pageNumber++;
            }
        }

        private async Task ThenTheMatchingPublishedProviderDocumentsAreDeleted(IEnumerable<PublishedProviderIndex> searchDocuments)
        {
            await _publishedProviderSearch
                .Received(1)
                .Remove(Arg.Is<IEnumerable<PublishedProviderIndex>>(_ => _.SequenceEqual(searchDocuments)));
        }
        
        private async Task AndTheMatchingPublishedProviderDocumentsAreDeleted(IEnumerable<PublishedProviderIndex> searchDocuments)
        {
            await ThenTheMatchingPublishedProviderDocumentsAreDeleted(searchDocuments);
        }

        private async Task ThenTheMatchingPublishedFundingDocumentsAreDeleted(IEnumerable<PublishedFundingIndex> searchDocuments)
        {
            await _publishedFundingSearch
                .Received(1)
                .Remove(Arg.Is<IEnumerable<PublishedFundingIndex>>(_ => _.SequenceEqual(searchDocuments)));
        }
        
        private async Task AndTheMatchingPublishedFundingDocumentsAreDeleted(IEnumerable<PublishedFundingIndex> searchDocuments)
        {
            await ThenTheMatchingPublishedFundingDocumentsAreDeleted(searchDocuments);
        }
        
        private async Task WhenTheFundingSearchDocumentsAreDeleted<TIndex>()
        where TIndex : class
        {
            await _service.DeleteFundingSearchDocuments<TIndex>(_fundingStreamId,
                _fundingPeriodId);
        }
        
        private string NewRandomString() => new RandomString();

        private PublishedProviderIndex NewPublishedProviderIndex()
        {
            return new PublishedProviderIndexBuilder().Build();
        }
        
        private PublishedFundingIndex NewPublishedFundingIndex()
        {
            return new PublishedFundingIndexBuilder().Build();
        }
    }
}