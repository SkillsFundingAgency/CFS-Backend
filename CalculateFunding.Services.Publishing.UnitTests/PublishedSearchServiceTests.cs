using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Repositories.Common.Search.Results;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Search.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Serilog;

namespace CalculateFunding.Services.Publishing.UnitTests
{
    [TestClass]
    public class PublishedSearchServiceTests
    {
        private HttpRequest _request;
        private ILogger _logger;
        private ISearchRepository<PublishedProviderIndex> _searchRepository;

        private PublishedSearchService _service;

        [TestInitialize]
        public void SetUp()
        {
            _request = Substitute.For<HttpRequest>();
            _logger = Substitute.For<ILogger>();
            _searchRepository = Substitute.For<ISearchRepository<PublishedProviderIndex>>();

            _service = new PublishedSearchService(_searchRepository,
                _logger);
        }

        [TestMethod]
        public async Task SearchPublishedProviders_GivenNullSearchModel_ReturnsBadRequest()
        {
            IActionResult result = await WhenTheSearchIsMade();

            result
                .Should()
                .BeOfType<BadRequestObjectResult>();

            ThenTheErrorWasLogged("A null or invalid search model was provided for searching published providers");
        }

        [TestMethod]
        [DataRow(0, 1)]
        [DataRow(1, 0)]
        public async Task SearchPublishedProviders_GivenInvalidPageNumberOrTop_ReturnsBadRequest(
            int pageNumber,
            int top)
        {
            GivenTheSearchModel(NewSearchModel(_ => _.WithPageNumber(pageNumber)
                .WithTop(top)));

            IActionResult result = await WhenTheSearchIsMade();

            result
                .Should()
                .BeOfType<BadRequestObjectResult>();

            ThenTheErrorWasLogged("A null or invalid search model was provided for searching published providers");
        }

        [TestMethod]
        public async Task DelegatesToSearchRepositoryAndMapsResultsIntoOkObjectResult()
        {
            SearchModel searchModel = NewSearchModel(_ => _.WithTop(50)
                .WithPageNumber(1)
                .WithSearchTerm(NewRandomString())
                .WithIncludeFacets(true)
                .AddFilter("filter1", "filter1value1", "filter1value2")
                .AddFilter("providerType", "test", ""));

            GivenTheSearchModel(searchModel);

            SearchResults<PublishedProviderIndex> searchIndexResults = NewSearchResults(_ =>
                _.WithResults(NewPublishedProviderIndex(),
                    NewPublishedProviderIndex(),
                    NewPublishedProviderIndex()));
            AndTheSearchResults(searchModel, searchIndexResults);

            OkObjectResult result = await WhenTheSearchIsMade() as OkObjectResult;

            result
                .Should()
                .NotBeNull();

            IEnumerable<PublishedSearchResult> publishedSearchResults = result.Value as IEnumerable<PublishedSearchResult>;

            publishedSearchResults?.Select(_ => _.Id)
                .Should()
                .BeEquivalentTo(searchIndexResults.Results.Select(_ => _.Result.Id));

            await ThenTheFiltersWasSearched(searchModel, 
                3, 
                "(providerType eq 'test' or providerType eq '') and (filter1 eq 'filter1value1' or filter1 eq 'filter1value2')");
            await AndTheFilterWasSearched(searchModel,
                1,
                "(filter1 eq 'filter1value1' or filter1 eq 'filter1value2')");
        }

        [TestMethod]
        [DataRow(10, 50, 450)]
        [DataRow(9, 100, 800)]
        [DataRow(5, 200, 800)]
        public async Task PagingSkipsPageSizeByPageNumberLessOneRecords(int pageNumber,
            int pageSize,
            int expectedRowsSkipped)
        {
            SearchModel searchModel = NewSearchModel(_ => _.WithTop(pageSize)
                .WithPageNumber(pageNumber));

            GivenTheSearchModel(searchModel);
            AndTheSearchResults(searchModel, new SearchResults<PublishedProviderIndex>());

            await WhenTheSearchIsMade();

            await ThenTheRowsWereSkippedDuringSearch(expectedRowsSkipped, 1, searchModel.SearchTerm);
        }

        [TestMethod]
        public async Task GetsPublishedProviderLocalAuthorities()
        {
            string fundingStreamId = "fundingStreamId1";
            string fundingPeriodId = "fundingPeriodId1";
            string searchText = "Der";

            string matchingFacetName = "Derby";
            string unmatchingFacetName = "Kent";
            string matchingTwoWordFacetName = "North Derby";

            SearchResults<PublishedProviderIndex> searchResults = new SearchResults<PublishedProviderIndex>
            {
                Facets = new List<Facet>
                {
                    new Facet
                    {
                        Name = "localAuthority",
                        FacetValues = new List<FacetValue>
                        { 
                            new FacetValue
                            {
                                Name = matchingFacetName
                            },
                            new FacetValue
                            {
                                Name = unmatchingFacetName
                            },
                            new FacetValue
                            {
                                Name = matchingTwoWordFacetName
                            },
                        } 
                    }
                }
            };

            GivenSearchResultsForLocalAuthority(searchText, searchResults);

            OkObjectResult result = await WhenSearchLocalAuthorityIsMade(searchText, fundingStreamId, fundingPeriodId) as OkObjectResult;

            result
                .Should()
                .NotBeNull();

            await ThenFacetValuesWasSearched(searchText, fundingStreamId, fundingPeriodId);

            IEnumerable<string> publishedProviderLocalAuthorities = result.Value as IEnumerable<string>;

            publishedProviderLocalAuthorities
                .Should()
                .HaveCount(2);

            publishedProviderLocalAuthorities
                .First()
                .Should()
                .BeEquivalentTo(matchingFacetName);

            publishedProviderLocalAuthorities
                .Last()
                .Should()
                .BeEquivalentTo(matchingTwoWordFacetName);
        }

        private SearchModel NewSearchModel(Action<SearchModelBuilder> setUp = null)
        {
            SearchModelBuilder searchModelBuilder = new SearchModelBuilder();

            setUp?.Invoke(searchModelBuilder);

            return searchModelBuilder.Build();
        }

        private void GivenTheSearchModel(SearchModel searchModel)
        {
            _request
                .Body
                .Returns(new MemoryStream(searchModel.AsJsonBytes()));
        }

        private async Task<IActionResult> WhenTheSearchIsMade()
        {
            return await _service.SearchPublishedProviders(_request);
        }

        private async Task<IActionResult> WhenSearchLocalAuthorityIsMade(string searchText, string fundingStreamId = null, string fundingPeriodId = null)
        {
            return await _service.SearchPublishedProviderLocalAuthorities(searchText, fundingStreamId, fundingPeriodId);
        }

        private void ThenTheErrorWasLogged(string error)
        {
            _logger
                .Received(1)
                .Error(error);
        }

        private string NewRandomString()
        {
            return new RandomString();
        }

        private SearchResults<PublishedProviderIndex> NewSearchResults(Action<SearchResultsBuilder<PublishedProviderIndex>> setUp = null)
        {
            SearchResultsBuilder<PublishedProviderIndex> searchResultsBuilder = new SearchResultsBuilder<PublishedProviderIndex>();

            setUp?.Invoke(searchResultsBuilder);

            return searchResultsBuilder.Build();
        }

        private PublishedProviderIndex NewPublishedProviderIndex(Action<PublishedProviderIndexBuilder> setUp = null)
        {
            PublishedProviderIndexBuilder publishedProviderIndexBuilder = new PublishedProviderIndexBuilder();

            setUp?.Invoke(publishedProviderIndexBuilder);

            return publishedProviderIndexBuilder.Build();
        }

        private async Task ThenTheFiltersWasSearched(SearchModel searchModel,
            int searchCallTimes,
            string expectedFilterLiteral)
        {
            await AndTheFilterWasSearched(searchModel, searchCallTimes, expectedFilterLiteral);
        }

        private async Task AndTheFilterWasSearched(SearchModel searchModel,
            int searchCallTimes,
            string expectedFilterLiteral)
        {
            await _searchRepository
                .Received(searchCallTimes)
                .Search(Arg.Is(searchModel.SearchTerm),
                    Arg.Is<SearchParameters>(_ =>
                        _.Filter == expectedFilterLiteral));
        }

        private async Task ThenTheRowsWereSkippedDuringSearch(int expectedSkipCount,
            int searchCalls = 4,
            string searchText = null)
        {
            searchText = searchText ?? "";

            await _searchRepository
                .Received(searchCalls)
                .Search(Arg.Is(searchText),
                    Arg.Is<SearchParameters>(_ => _.Skip == expectedSkipCount),
                    Arg.Is(false));
        }

        private void AndTheSearchResults(SearchModel search, SearchResults<PublishedProviderIndex> results)
        {
            _searchRepository.Search(Arg.Is(search.SearchTerm), Arg.Any<SearchParameters>())
                .Returns(results);
        }

        private void GivenSearchResultsForLocalAuthority(string searchText, SearchResults<PublishedProviderIndex> results)
        {
            _searchRepository.Search(Arg.Any<string>(), Arg.Any<SearchParameters>())
                .Returns(results);
        }

        private async Task ThenFacetValuesWasSearched(string searchText, string fundingStreamId, string fundingPeriodId)
        {
            await _searchRepository
                    .Received(1)
                    .Search(string.Empty,
                        Arg.Is<SearchParameters>(_ => 
                        _.Top == 0 && 
                        _.Filter == $"(fundingStreamId eq '{fundingStreamId}') and (fundingPeriodId eq '{fundingPeriodId}')" &&
                        _.Facets.First().StartsWith("localAuthority")));
        }
    }
}