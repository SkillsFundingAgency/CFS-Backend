using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CalculateFunding.Models;
using CalculateFunding.Models.Results.Search;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.TestRunner.Interfaces;
using CalculateFunding.Services.TestRunner.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Search.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using NSubstitute;
using Serilog;

namespace CalculateFunding.Services.TestRunner.UnitTests
{
    [TestClass]
    public class TestResultsSearchServiceTests
    {
        [TestMethod]
        public async Task SearchTestScenarioResults_SearchRequestFails_ThenBadRequestReturned()
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

            ISearchRepository<TestScenarioResultIndex> searchRepository = CreateSearchRepository();

            searchRepository
                .When(s => s.Search(Arg.Any<string>(), Arg.Any<SearchParameters>()))
                    .Do(x => { throw new FailedToQuerySearchException("Test Message", null); });


            TestResultsSearchService service = CreateTestResultsSearchService(logger: logger, serachRepository: searchRepository);

            //Act
            IActionResult result = await service.SearchTestScenarioResults(request);

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
        public async Task SearchTestScenarioResults_GivenNullSearchModel_LogsAndCreatesDefaultSearcModel()
        {
            //Arrange
            HttpRequest request = Substitute.For<HttpRequest>();

            ILogger logger = CreateLogger();

            ISearchRepository<TestScenarioResultIndex> searchRepository = CreateSearchRepository();

            TestResultsSearchService service = CreateTestResultsSearchService(logger: logger, serachRepository: searchRepository);

            //Act
            IActionResult result = await service.SearchTestScenarioResults(request);

            //Assert
            logger
                .Received(1)
                .Error("A null or invalid search model was provided for searching calculations");

            result
                 .Should()
                 .BeOfType<BadRequestObjectResult>();
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

            ISearchRepository<TestScenarioResultIndex> searchRepository = CreateSearchRepository();

            TestResultsSearchService service = CreateTestResultsSearchService(logger: logger, serachRepository: searchRepository);

            //Act
            IActionResult result = await service.SearchTestScenarioResults(request);

            //Assert
            logger
                .Received(1)
                .Error("A null or invalid search model was provided for searching calculations");

            result
                 .Should()
                 .BeOfType<BadRequestObjectResult>();
        }

        [TestMethod]
        public async Task SearchTestScenarioResults_GivenPageTopIsZero_LogsAndReturnsBadRequest()
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

            ISearchRepository<TestScenarioResultIndex> searchRepository = CreateSearchRepository();

            TestResultsSearchService service = CreateTestResultsSearchService(logger: logger, serachRepository: searchRepository);

            //Act
            IActionResult result = await service.SearchTestScenarioResults(request);

            //Assert
            logger
                .Received(1)
                .Error("A null or invalid search model was provided for searching calculations");

            result
                 .Should()
                 .BeOfType<BadRequestObjectResult>();
        }

        [TestMethod]
        public async Task SearchTestScenarioResults_GivenValidModelAndIncludesGettingFacets_CallsSearchSevenTimes()
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

            SearchResults<TestScenarioResultIndex> searchResults = new SearchResults<TestScenarioResultIndex>();

            ILogger logger = CreateLogger();

            ISearchRepository<TestScenarioResultIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .Search(Arg.Any<string>(), Arg.Any<SearchParameters>())
                .Returns(searchResults);

            TestResultsSearchService service = CreateTestResultsSearchService(logger: logger, serachRepository: searchRepository);

            //Act
            IActionResult result = await service.SearchTestScenarioResults(request);

            //Assert
            result
                 .Should()
                 .BeOfType<OkObjectResult>();

            await
                searchRepository
                    .Received(11)
                    .Search(Arg.Any<string>(), Arg.Any<SearchParameters>());
        }

        [TestMethod]
        public async Task SearchTestScenarioResults_GivenValidModelWithNullFilters_ThenSearchIsStillPerformed()
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

            SearchResults<TestScenarioResultIndex> searchResults = new SearchResults<TestScenarioResultIndex>();

            ILogger logger = CreateLogger();

            ISearchRepository<TestScenarioResultIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .Search(Arg.Any<string>(), Arg.Any<SearchParameters>())
                .Returns(searchResults);

            TestResultsSearchService service = CreateTestResultsSearchService(logger: logger, serachRepository: searchRepository);

            //Act
            IActionResult result = await service.SearchTestScenarioResults(request);

            //Assert
            result
                 .Should()
                 .BeOfType<OkObjectResult>();

            await
                searchRepository
                    .Received(11)
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
                    { "providerId", new string []{ "test" } }
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

            SearchResults<TestScenarioResultIndex> searchResults = new SearchResults<TestScenarioResultIndex>();

            ILogger logger = CreateLogger();

            ISearchRepository<TestScenarioResultIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .Search(Arg.Any<string>(), Arg.Any<SearchParameters>())
                .Returns(searchResults);

            TestResultsSearchService service = CreateTestResultsSearchService(logger: logger, serachRepository: searchRepository);

            //Act
            IActionResult result = await service.SearchTestScenarioResults(request);

            //Assert
            result
                 .Should()
                 .BeOfType<OkObjectResult>();

            await
                searchRepository
                .Received(10)
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
                    { "providerId", new string []{ "test" } }
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

            SearchResults<TestScenarioResultIndex> searchResults = new SearchResults<TestScenarioResultIndex>();

            ILogger logger = CreateLogger();

            ISearchRepository<TestScenarioResultIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .Search(Arg.Any<string>(), Arg.Any<SearchParameters>())
                .Returns(searchResults);

            TestResultsSearchService service = CreateTestResultsSearchService(logger: logger, serachRepository: searchRepository);

            //Act
            IActionResult result = await service.SearchTestScenarioResults(request);

            //Assert
            result
                 .Should()
                 .BeOfType<OkObjectResult>();

            await
                searchRepository
                .Received(10)
                    .Search(model.SearchTerm, Arg.Is<SearchParameters>(c =>
                        model.Filters.Keys.All(f => c.Filter.Contains(f))
                        && !string.IsNullOrWhiteSpace(c.Filter)
                    ));
        }

        [TestMethod]
        public async Task SearchTestScenarioResults_GivenValidModelWithMultipleFilterValuesWhichIsMultiValueFacet_ThenSearchIsPerformed()
        {
            //Arrange
            SearchModel model = new SearchModel
            {
                PageNumber = 1,
                Top = 50,
                IncludeFacets = true,
                Filters = new Dictionary<string, string[]>()
                {
                    { "providerId", new string []{ "test", "test2" } }
                },
                SearchTerm = "testTerm",
                SearchFields = new[] { "test" }
            };

            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Body
                .Returns(stream);

            SearchResults<TestScenarioResultIndex> searchResults = new SearchResults<TestScenarioResultIndex>();

            ILogger logger = CreateLogger();

            ISearchRepository<TestScenarioResultIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .Search(Arg.Any<string>(), Arg.Any<SearchParameters>())
                .Returns(searchResults);

            TestResultsSearchService service = CreateTestResultsSearchService(logger: logger, serachRepository: searchRepository);

            //Act
            IActionResult result = await service.SearchTestScenarioResults(request);

            //Assert
            result
                 .Should()
                 .BeOfType<OkObjectResult>();

            await
                searchRepository
                .Received(10)
                    .Search(model.SearchTerm, Arg.Is<SearchParameters>(c =>
                       model.Filters.Keys.All(f => c.Filter.Contains(f))
                       && !string.IsNullOrWhiteSpace(c.Filter)
                    ));
        }

        [TestMethod]
        public async Task SearchTestScenarioResults_GivenValidModelWithMultipleOfSameFilter_ThenSearchIsPerformed()
        {
            //Arrange
            SearchModel model = new SearchModel
            {
                PageNumber = 1,
                Top = 50,
                IncludeFacets = true,
                Filters = new Dictionary<string, string[]>()
                {
                    { "providerId", new string []{ "test", "test2" } }
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

            SearchResults<TestScenarioResultIndex> searchResults = new SearchResults<TestScenarioResultIndex>();

            ILogger logger = CreateLogger();

            ISearchRepository<TestScenarioResultIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .Search(Arg.Any<string>(), Arg.Any<SearchParameters>())
                .Returns(searchResults);

            TestResultsSearchService service = CreateTestResultsSearchService(logger: logger, serachRepository: searchRepository);

            //Act
            IActionResult result = await service.SearchTestScenarioResults(request);

            //Assert
            result
                 .Should()
                 .BeOfType<OkObjectResult>();

            await
                searchRepository
                .Received(10)
                    .Search(model.SearchTerm, Arg.Is<SearchParameters>(c =>
                        model.Filters.Keys.All(f => c.Filter.Contains(f))
                        && !string.IsNullOrWhiteSpace(c.Filter)
                    ));
        }

        [TestMethod]
        public async Task SearchTestScenarioResults_GivenValidModelWithNullFilterWithMultipleOfSameFilter_ThenSearchIsPerformed()
        {
            //Arrange
            SearchModel model = new SearchModel
            {
                PageNumber = 1,
                Top = 50,
                IncludeFacets = true,
                Filters = new Dictionary<string, string[]>()
                {
                    { "providerId", new string []{ "test", "" } }
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

            SearchResults<TestScenarioResultIndex> searchResults = new SearchResults<TestScenarioResultIndex>();

            ILogger logger = CreateLogger();

            ISearchRepository<TestScenarioResultIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .Search(Arg.Any<string>(), Arg.Any<SearchParameters>())
                .Returns(searchResults);

            TestResultsSearchService service = CreateTestResultsSearchService(logger: logger, serachRepository: searchRepository);

            //Act
            IActionResult result = await service.SearchTestScenarioResults(request);

            //Assert
            result
                 .Should()
                 .BeOfType<OkObjectResult>();

            await
                searchRepository
                .Received(10)
                    .Search(model.SearchTerm, Arg.Is<SearchParameters>(c =>
                        model.Filters.Keys.All(f => c.Filter.Contains(f))
                        && !string.IsNullOrWhiteSpace(c.Filter)
                    ));
        }

        [TestMethod]
        public async Task SearchTestScenarioResults_GivenValidModelAndDoesntIncludeGettingFacets_CallsSearchOnce()
        {
            //Arrange
            SearchModel model = new SearchModel
            {
                PageNumber = 1,
                Top = 50
            };

            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Body
                .Returns(stream);

            SearchResults<TestScenarioResultIndex> searchResults = new SearchResults<TestScenarioResultIndex>();

            ILogger logger = CreateLogger();

            ISearchRepository<TestScenarioResultIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .Search(Arg.Any<string>(), Arg.Any<SearchParameters>())
                .Returns(searchResults);

            TestResultsSearchService service = CreateTestResultsSearchService(logger: logger, serachRepository: searchRepository);

            //Act
            IActionResult result = await service.SearchTestScenarioResults(request);

            //Assert
            result
                 .Should()
                 .BeOfType<OkObjectResult>();

            await
                searchRepository
                    .Received(1)
                    .Search(Arg.Any<string>(), Arg.Any<SearchParameters>());
        }

        [TestMethod]
        public async Task SearchTestScenarioResults_GivenValidModelAndPageNumber2_CallsSearchWithCorrectSkipValue()
        {
            //Arrange
            const int skipValue = 50;

            SearchModel model = new SearchModel
            {
                PageNumber = 2,
                Top = 50
            };

            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Body
                .Returns(stream);

            SearchResults<TestScenarioResultIndex> searchResults = new SearchResults<TestScenarioResultIndex>();

            ILogger logger = CreateLogger();

            ISearchRepository<TestScenarioResultIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .Search(Arg.Any<string>(), Arg.Any<SearchParameters>())
                .Returns(searchResults);

            TestResultsSearchService service = CreateTestResultsSearchService(logger: logger, serachRepository: searchRepository);

            //Act
            IActionResult result = await service.SearchTestScenarioResults(request);

            //Assert
            result
                 .Should()
                 .BeOfType<OkObjectResult>();

            await
                searchRepository
                    .Received(1)
                    .Search(Arg.Any<string>(), Arg.Is<SearchParameters>(m => m.Skip == skipValue));
        }

        [TestMethod]
        public async Task SearchTestScenarioResults_GivenValidModelAndPageNumber10_CallsSearchWithCorrectSkipValue()
        {
            //Arrange
            const int skipValue = 450;

            SearchModel model = new SearchModel
            {
                PageNumber = 10,
                Top = 50
            };

            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Body
                .Returns(stream);

            SearchResults<TestScenarioResultIndex> searchResults = new SearchResults<TestScenarioResultIndex>();

            ILogger logger = CreateLogger();

            ISearchRepository<TestScenarioResultIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .Search(Arg.Any<string>(), Arg.Any<SearchParameters>())
                .Returns(searchResults);

            TestResultsSearchService service = CreateTestResultsSearchService(logger: logger, serachRepository: searchRepository);

            //Act
            IActionResult result = await service.SearchTestScenarioResults(request);

            //Assert
            result
                 .Should()
                 .BeOfType<OkObjectResult>();

            await
                searchRepository
                    .Received(1)
                    .Search(Arg.Any<string>(), Arg.Is<SearchParameters>(m => m.Skip == skipValue));
        }

        [TestMethod]
        public async Task SearchTestScenarioResults_GivenValidModel_CallsSearchWithCorrectSkipValue()
        {
            //Arrange
            SearchModel model = new SearchModel
            {
                PageNumber = 10,
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

            SearchResults<TestScenarioResultIndex> searchResults = new SearchResults<TestScenarioResultIndex>
            {
                Facets = new List<Facet>
                {
                    new Facet
                    {
                        Name = "testResult"
                    }
                }
            };

            ILogger logger = CreateLogger();

            ISearchRepository<TestScenarioResultIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .Search(Arg.Any<string>(), Arg.Any<SearchParameters>())
                .Returns(searchResults);

            TestResultsSearchService service = CreateTestResultsSearchService(logger: logger, serachRepository: searchRepository);

            //Act
            IActionResult result = await service.SearchTestScenarioResults(request);

            //Assert
            result
                 .Should()
                 .BeOfType<OkObjectResult>();

            await
                searchRepository
                    .Received(11)
                    .Search(Arg.Any<string>(), Arg.Any<SearchParameters>());
        }

        static TestResultsSearchService CreateTestResultsSearchService(
           ILogger logger = null,
           ISearchRepository<TestScenarioResultIndex> serachRepository = null,
           ITestRunnerResiliencePolicies resiliencePolicies = null)
        {
            return new TestResultsSearchService(
                logger ?? CreateLogger(),
                serachRepository ?? CreateSearchRepository(),
                resiliencePolicies ?? TestRunnerResilienceTestHelper.GenerateTestPolicies());
        }

        static ILogger CreateLogger()
        {
            return Substitute.For<ILogger>();
        }

        static ISearchRepository<TestScenarioResultIndex> CreateSearchRepository()
        {
            return Substitute.For<ISearchRepository<TestScenarioResultIndex>>();
        }
    }
}
