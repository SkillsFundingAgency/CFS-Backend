using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CalculateFunding.Common.FeatureToggles;
using CalculateFunding.Models;
using CalculateFunding.Models.Results.Search;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Results.Interfaces;
using CalculateFunding.Services.Results.UnitTests;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Search.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using NSubstitute;
using Serilog;

namespace CalculateFunding.Services.Results.Services
{
    [TestClass]
    public class ProviderCalculationResultsSearchServiceTests
    {
        [TestMethod]
        public async Task SearchCalculationProviderResults_GivenNullModel_ReturnsBadRequest()
        {
            //Arrange
            HttpRequest httpRequest = Substitute.For<HttpRequest>();

            ILogger logger = CreateLogger();

            ProviderCalculationResultsSearchService service = CreateTestResultsSearchService(logger: logger);

            //Act
            IActionResult result = await service.SearchCalculationProviderResults(httpRequest);

            //Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>();

            logger
                .Received(1)
                .Error(Arg.Is("A null or invalid search model was provided for searching calculation provider results"));
        }

        [TestMethod]
        public async Task SearchTestScenarioResults_GivenPageNumberIsZero_LogsAndReturnsBadRequest()
        {
            //Arrange
            SearchModel model = new SearchModel();
            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Body
                .Returns(stream);

            ILogger logger = CreateLogger();

            ISearchRepository<ProviderCalculationResultsIndex> searchRepository = CreateSearchRepository();

            ProviderCalculationResultsSearchService service = CreateTestResultsSearchService(logger, searchRepository);

            //Act
            IActionResult result = await service.SearchCalculationProviderResults(request);

            //Assert
            logger
                .Received(1)
                .Error("A null or invalid search model was provided for searching calculation provider results");

            result
                 .Should()
                 .BeOfType<BadRequestObjectResult>();
        }

        [TestMethod]
        public async Task SearchTestScenarioResults_GivenTopIsZero_LogsAndReturnsBadRequest()
        {
            //Arrange
            SearchModel model = new SearchModel
            {
                PageNumber = 1,
                Top = 0
            };
            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Body
                .Returns(stream);

            ILogger logger = CreateLogger();

            ISearchRepository<ProviderCalculationResultsIndex> searchRepository = CreateSearchRepository();

            ProviderCalculationResultsSearchService service = CreateTestResultsSearchService(logger, searchRepository);

            //Act
            IActionResult result = await service.SearchCalculationProviderResults(request);

            //Assert
            logger
                .Received(1)
                .Error("A null or invalid search model was provided for searching calculation provider results");

            result
                 .Should()
                 .BeOfType<BadRequestObjectResult>();
        }

        [TestMethod]
        public async Task SearchCalculationProviderResults_GivenSearchRequestFails_ThenBadRequestReturned()
        {
            //Arrange
            SearchModel model = new SearchModel()
            {
                SearchTerm = "SearchTermTest",
                PageNumber = 1,
                IncludeFacets = false,
                Top = 50,
            };

            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Body
                .Returns(stream);

            ILogger logger = CreateLogger();

            ISearchRepository<ProviderCalculationResultsIndex> searchRepository = CreateSearchRepository();

            searchRepository
                .When(s => s.Search(Arg.Any<string>(), Arg.Any<SearchParameters>()))
                    .Do(x => { throw new FailedToQuerySearchException("Test Message", null); });


            ProviderCalculationResultsSearchService service = CreateTestResultsSearchService(logger, searchRepository);

            //Act
            IActionResult result = await service.SearchCalculationProviderResults(request);

            //Assert
            logger
                .Received(1)
                .Error(Arg.Any<FailedToQuerySearchException>(), "Failed to query search with term: SearchTermTest");

            result
                 .Should()
                 .BeOfType<StatusCodeResult>()
                 .Which.StatusCode.Should().Be(500);
        }

        [TestMethod]
        public async Task SearchTestScenarioResults_GivenValidModelAndIncludesGettingFacets_CallsSearchTenTimes()
        {
            //Arrange
            SearchModel model = new SearchModel
            {
                PageNumber = 1,
                Top = 50,
                IncludeFacets = true
            };

            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Body
                .Returns(stream);

            ILogger logger = CreateLogger();

            SearchResults<ProviderCalculationResultsIndex> searchResults = new SearchResults<ProviderCalculationResultsIndex>();

            ISearchRepository<ProviderCalculationResultsIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .Search(Arg.Any<string>(), Arg.Any<SearchParameters>())
                .Returns(searchResults);

            ProviderCalculationResultsSearchService service = CreateTestResultsSearchService(logger, searchRepository);

            //Act
            IActionResult result = await service.SearchCalculationProviderResults(request);

            //Assert
            result
                 .Should()
                 .BeOfType<OkObjectResult>();

            await
                searchRepository
                    .Received(10)
                    .Search(Arg.Any<string>(), Arg.Any<SearchParameters>());
        }

        [TestMethod]
        public async Task SearchTestScenarioResults_GivenValidModelAndIncludesGettingFacetsAndFiltersIsNull_PerformsSearch()
        {
            //Arrange
            SearchModel model = new SearchModel
            {
                PageNumber = 1,
                Top = 50,
                IncludeFacets = true,
                Filters = null,
            };
            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Body
                .Returns(stream);

            ILogger logger = CreateLogger();

            SearchResults<ProviderCalculationResultsIndex> searchResults = new SearchResults<ProviderCalculationResultsIndex>();

            ISearchRepository<ProviderCalculationResultsIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .Search(Arg.Any<string>(), Arg.Any<SearchParameters>())
                .Returns(searchResults);

            ProviderCalculationResultsSearchService service = CreateTestResultsSearchService(logger, searchRepository);

            //Act
            IActionResult result = await service.SearchCalculationProviderResults(request);

            //Assert
            result
                 .Should()
                 .BeOfType<OkObjectResult>();

            await
                searchRepository
                    .Received(10)
                    .Search(Arg.Any<string>(), Arg.Any<SearchParameters>());
        }

        [TestMethod]
        public async Task SearchTestScenarioResults_GivenValidModelWithOneFilter_ThenSearchIsPerformed()
        {
            //Arrange
            SearchModel model = new SearchModel
            {
                PageNumber = 1,
                Top = 50,
                IncludeFacets = true,
                Filters = new Dictionary<string, string[]>()
                {
                    { "calculationId", new string []{ "test" } }
                },
                SearchTerm = "testTerm",
            };

            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Body
                .Returns(stream);

            ILogger logger = CreateLogger();

            SearchResults<ProviderCalculationResultsIndex> searchResults = new SearchResults<ProviderCalculationResultsIndex>();

            ISearchRepository<ProviderCalculationResultsIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .Search(Arg.Any<string>(), Arg.Any<SearchParameters>())
                .Returns(searchResults);

            ProviderCalculationResultsSearchService service = CreateTestResultsSearchService(logger, searchRepository);

            //Act
            IActionResult result = await service.SearchCalculationProviderResults(request);

            //Assert
            result
                 .Should()
                 .BeOfType<OkObjectResult>();

            await
               searchRepository
               .Received(9)
                   .Search(model.SearchTerm, Arg.Is<SearchParameters>(c =>
                       model.Filters.Keys.All(f => c.Filter.Contains(f))
                       && !string.IsNullOrWhiteSpace(c.Filter)
                   ));
        }

        [TestMethod]
        public async Task SearchTestScenarioResults_GivenValidModelWithOneFilterWhichIsMultiValueFacet_ThenSearchIsPerformed()
        {
            //Arrange
            SearchModel model = new SearchModel
            {
                PageNumber = 1,
                Top = 50,
                IncludeFacets = true,
                Filters = new Dictionary<string, string[]>()
                {
                    { "calculationId", new string []{ "test" } }
                },
                SearchTerm = "testTerm",
            };

            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Body
                .Returns(stream);

            ILogger logger = CreateLogger();

            SearchResults<ProviderCalculationResultsIndex> searchResults = new SearchResults<ProviderCalculationResultsIndex>();

            ISearchRepository<ProviderCalculationResultsIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .Search(Arg.Any<string>(), Arg.Any<SearchParameters>())
                .Returns(searchResults);

            ProviderCalculationResultsSearchService service = CreateTestResultsSearchService(logger, searchRepository);

            //Act
            IActionResult result = await service.SearchCalculationProviderResults(request);

            //Assert
            result
                 .Should()
                 .BeOfType<OkObjectResult>();

            await
                searchRepository
                .Received(9)
                    .Search(model.SearchTerm, Arg.Is<SearchParameters>(c =>
                        model.Filters.Keys.All(f => c.Filter.Contains(f))
                        && !string.IsNullOrWhiteSpace(c.Filter)
                    ));
        }

        [TestMethod]
        public async Task SearchTestScenarioResults_GivenValidModelWithOneFilterAndOverrridesFacets_ThenSearchIsPerformedWithTwoFilters()
        {
            //Arrange
            SearchModel model = new SearchModel
            {
                PageNumber = 1,
                Top = 50,
                IncludeFacets = true,
                Filters = new Dictionary<string, string[]>()
                {
                    { "calculationId", new string []{ "test" } }
                },
                SearchTerm = "testTerm",
                OverrideFacetFields = new[] { "providerId", "calculationId" }
            };

            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Body
                .Returns(stream);

            ILogger logger = CreateLogger();

            SearchResults<ProviderCalculationResultsIndex> searchResults = new SearchResults<ProviderCalculationResultsIndex>();

            ISearchRepository<ProviderCalculationResultsIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .Search(Arg.Any<string>(), Arg.Any<SearchParameters>())
                .Returns(searchResults);

            ProviderCalculationResultsSearchService service = CreateTestResultsSearchService(logger, searchRepository);

            //Act
            IActionResult result = await service.SearchCalculationProviderResults(request);

            //Assert
            result
                 .Should()
                 .BeOfType<OkObjectResult>();

            await
                searchRepository
                .Received(2)
                    .Search(model.SearchTerm, Arg.Is<SearchParameters>(c =>
                        model.Filters.Keys.All(f => c.Filter.Contains(f))
                        && !string.IsNullOrWhiteSpace(c.Filter)
                    ));
        }

        static ProviderCalculationResultsSearchService CreateTestResultsSearchService(
          ILogger logger = null,
          ISearchRepository<ProviderCalculationResultsIndex> serachRepository = null,
          IResultsResilliencePolicies resiliencePolicies = null)
        {
            return new ProviderCalculationResultsSearchService(
                logger ?? CreateLogger(),
                serachRepository ?? CreateSearchRepository(),
                resiliencePolicies ?? ResultsResilienceTestHelper.GenerateTestPolicies());
        }

        static ILogger CreateLogger()
        {
            return Substitute.For<ILogger>();
        }

        static ISearchRepository<ProviderCalculationResultsIndex> CreateSearchRepository()
        {
            return Substitute.For<ISearchRepository<ProviderCalculationResultsIndex>>();
        }
    }
}
