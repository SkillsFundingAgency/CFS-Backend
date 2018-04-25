using CalculateFunding.Models;
using CalculateFunding.Models.Scenarios;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Repositories.Common.Search.Results;
using CalculateFunding.Services.TestRunner.Interfaces;
using CalculateFunding.Services.TestRunner.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using NSubstitute;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.TestRunner.UnitTests
{
    [TestClass]
    public class TestResultsCountServiceTests
    {
        [TestMethod]
        public async Task GetResultCounts_GivenNullModel_ReturnsBadRequest()
        {
            //Arrange
            HttpRequest httpRequest = Substitute.For<HttpRequest>();

            ILogger logger = CreateLogger();

            TestResultsCountsService service = CreateResultCountsService(logger: logger);

            //Act
            IActionResult result = await service.GetResultCounts(httpRequest);

            //Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>();

            logger
                .Received(1)
                .Error(Arg.Is("Null or empty test scenario ids provided"));
        }

        [TestMethod]
        public async Task GetResultCounts_GivenModelWithNullScenariIds_ReturnsBadRequest()
        {
            //Arrange
            TestScenariosResultsCountsRequestModel model = new TestScenariosResultsCountsRequestModel();
            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            HttpRequest httpRequest = Substitute.For<HttpRequest>();
            httpRequest
                .Body
                .Returns(stream);

            ILogger logger = CreateLogger();

            TestResultsCountsService service = CreateResultCountsService(logger: logger);

            //Act
            IActionResult result = await service.GetResultCounts(httpRequest);

            //Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>();

            logger
                .Received(1)
                .Error(Arg.Is("Null or empty test scenario ids provided"));
        }

        [TestMethod]
        public async Task GetResultCounts_GivenModelWithEmptyScenariIds_ReturnsBadRequest()
        {
            //Arrange
            TestScenariosResultsCountsRequestModel model = new TestScenariosResultsCountsRequestModel
            {
                TestScenarioIds = Enumerable.Empty<string>()
            };

            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            HttpRequest httpRequest = Substitute.For<HttpRequest>();
            httpRequest
                .Body
                .Returns(stream);

            ILogger logger = CreateLogger();

            TestResultsCountsService service = CreateResultCountsService(logger: logger);

            //Act
            IActionResult result = await service.GetResultCounts(httpRequest);

            //Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>();

            logger
                .Received(1)
                .Error(Arg.Is("Null or empty test scenario ids provided"));
        }

        [TestMethod]
        public async Task GetResultCounts_GivenModelWithScenarioIdsButBothResultsReturnNull_ReturnsOKWithNoResult()
        {
            //Arrange
            TestScenariosResultsCountsRequestModel model = new TestScenariosResultsCountsRequestModel
            {
                TestScenarioIds = new[] { "1", "2" }
            };

            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            HttpRequest httpRequest = Substitute.For<HttpRequest>();
            httpRequest
                .Body
                .Returns(stream);

            ILogger logger = CreateLogger();

            ITestResultsSearchService searchService = CreatetestResultsSearchService();
            searchService
                .SearchTestScenarioResults(Arg.Any<SearchModel>())
                .Returns((TestScenarioSearchResults)null);

            TestResultsCountsService service = CreateResultCountsService(searchService, logger);

            //Act
            IActionResult result = await service.GetResultCounts(httpRequest);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();

            OkObjectResult okResult = result as OkObjectResult;

            IList<TestScenarioResultCounts> results = okResult.Value as IList<TestScenarioResultCounts>;

            results
                .Count
                .Should()
                .Be(0);
        }

        [TestMethod]
        public async Task GetResultCounts_GivenModelWithScenarioIdsButBothResultsButNoFacets_ReturnsOKWithNoResult()
        {
            //Arrange
            TestScenariosResultsCountsRequestModel model = new TestScenariosResultsCountsRequestModel
            {
                TestScenarioIds = new[] { "1", "2" }
            };

            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            HttpRequest httpRequest = Substitute.For<HttpRequest>();
            httpRequest
                .Body
                .Returns(stream);

            ILogger logger = CreateLogger();

            TestScenarioSearchResults testScenarioSearchResults1 = new TestScenarioSearchResults();
            TestScenarioSearchResults testScenarioSearchResults2 = new TestScenarioSearchResults();

            ITestResultsSearchService searchService = CreatetestResultsSearchService();
            searchService
                .SearchTestScenarioResults(Arg.Any<SearchModel>())
                .Returns(testScenarioSearchResults1, testScenarioSearchResults2);

            TestResultsCountsService service = CreateResultCountsService(searchService, logger);

            //Act
            IActionResult result = await service.GetResultCounts(httpRequest);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();

            OkObjectResult okResult = result as OkObjectResult;

            IList<TestScenarioResultCounts> results = okResult.Value as IList<TestScenarioResultCounts>;

            results
                .Count
                .Should()
                .Be(0);
        }

        [TestMethod]
        public async Task GetResultCounts_GivenModelWithScenarioIdsButBothResultsButOnlyOneHasFacets_ReturnsOKWitOneResult()
        {
            //Arrange
            TestScenariosResultsCountsRequestModel model = new TestScenariosResultsCountsRequestModel
            {
                TestScenarioIds = new[] { "1", "2" }
            };

            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            HttpRequest httpRequest = Substitute.For<HttpRequest>();
            httpRequest
                .Body
                .Returns(stream);

            ILogger logger = CreateLogger();

            TestScenarioSearchResults testScenarioSearchResults1 = new TestScenarioSearchResults();
            TestScenarioSearchResults testScenarioSearchResults2 = new TestScenarioSearchResults
            {
                Results = new[]
                {
                    new TestScenarioSearchResult
                    {
                        TestScenarioName = "Test Name",
                        LastUpdatedDate = DateTimeOffset.Now
                    }
                },
                Facets = new[]
                {
                    new Facet{ Name = "testResult" }
                }
            };

            ITestResultsSearchService searchService = CreatetestResultsSearchService();
            searchService
                .SearchTestScenarioResults(Arg.Any<SearchModel>())
                .Returns(testScenarioSearchResults1, testScenarioSearchResults2);

            TestResultsCountsService service = CreateResultCountsService(searchService, logger);

            //Act
            IActionResult result = await service.GetResultCounts(httpRequest);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();

            OkObjectResult okResult = result as OkObjectResult;

            IList<TestScenarioResultCounts> results = okResult.Value as IList<TestScenarioResultCounts>;

            results
                .Count
                .Should()
                .Be(1);
        }

        [TestMethod]
        public async Task GetResultCounts_GivenModelWithScenarioIdss_ReturnsOKWitTwoResults()
        {
            //Arrange
            TestScenariosResultsCountsRequestModel model = new TestScenariosResultsCountsRequestModel
            {
                TestScenarioIds = new[] { "1", "2" }
            };

            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            HttpRequest httpRequest = Substitute.For<HttpRequest>();
            httpRequest
                .Body
                .Returns(stream);

            ILogger logger = CreateLogger();

            TestScenarioSearchResults testScenarioSearchResults1 = new TestScenarioSearchResults
            {
                Results = new[]
                {
                    new TestScenarioSearchResult
                    {
                        TestScenarioName = "Test Name 1",
                        LastUpdatedDate = DateTimeOffset.Now
                    }
                },
                Facets = new[]
                {
                    new Facet{ Name = "testResult" }
                }
            };

            TestScenarioSearchResults testScenarioSearchResults2 = new TestScenarioSearchResults
            {
                Results = new[]
               {
                    new TestScenarioSearchResult
                    {
                        TestScenarioName = "Test Name 2",
                        LastUpdatedDate = DateTimeOffset.Now
                    }
                },
                Facets = new[]
               {
                    new Facet{ Name = "testResult" }
                }
            };

            ITestResultsSearchService searchService = CreatetestResultsSearchService();
            searchService
                .SearchTestScenarioResults(Arg.Any<SearchModel>())
                .Returns(testScenarioSearchResults1, testScenarioSearchResults2);

            TestResultsCountsService service = CreateResultCountsService(searchService, logger);

            //Act
            IActionResult result = await service.GetResultCounts(httpRequest);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();

            OkObjectResult okResult = result as OkObjectResult;

            IList<TestScenarioResultCounts> results = okResult.Value as IList<TestScenarioResultCounts>;

            results
                .Count
                .Should()
                .Be(2);
        }

        [TestMethod]
        public async Task GetResultCounts_GivenModelWithScenarioIdsAndOnlyPassedWithCount_ReturnsOKeResult()
        {
            //Arrange
            TestScenariosResultsCountsRequestModel model = new TestScenariosResultsCountsRequestModel
            {
                TestScenarioIds = new[] { "1" }
            };

            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            HttpRequest httpRequest = Substitute.For<HttpRequest>();
            httpRequest
                .Body
                .Returns(stream);

            ILogger logger = CreateLogger();

            TestScenarioSearchResults testScenarioSearchResults1 = new TestScenarioSearchResults
            {
                Results = new[]
                {
                    new TestScenarioSearchResult
                    {
                        TestScenarioName = "Test Name",
                        LastUpdatedDate = DateTimeOffset.Now
                    }
                },
                Facets = new[]
                {
                    new Facet{ Name = "testResult", FacetValues = new[] { new FacetValue { Count = 10, Name = "Passed" } } }
                }
            };

            ITestResultsSearchService searchService = CreatetestResultsSearchService();
            searchService
                .SearchTestScenarioResults(Arg.Any<SearchModel>())
                .Returns(testScenarioSearchResults1);

            TestResultsCountsService service = CreateResultCountsService(searchService, logger);

            //Act
            IActionResult result = await service.GetResultCounts(httpRequest);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();

            OkObjectResult okResult = result as OkObjectResult;

            IList<TestScenarioResultCounts> results = okResult.Value as IList<TestScenarioResultCounts>;

            results
                .Count
                .Should()
                .Be(1);

            results
                .First()
                .Passed
                .Should()
                .Be(10);

            results
                .First()
                .Failed
                .Should()
                .Be(0);

            results
                .First()
                .Ignored
                .Should()
                .Be(0);
        }

        [TestMethod]
        public async Task GetResultCounts_GivenModelWithScenarioIdsWithAllFacetValues_ReturnsOKeResult()
        {
            //Arrange
            TestScenariosResultsCountsRequestModel model = new TestScenariosResultsCountsRequestModel
            {
                TestScenarioIds = new[] { "1" }
            };

            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            HttpRequest httpRequest = Substitute.For<HttpRequest>();
            httpRequest
                .Body
                .Returns(stream);

            ILogger logger = CreateLogger();

            TestScenarioSearchResults testScenarioSearchResults1 = new TestScenarioSearchResults
            {
                Results = new[]
                {
                    new TestScenarioSearchResult
                    {
                        TestScenarioName = "Test Name",
                        LastUpdatedDate = DateTimeOffset.Now
                    }
                },
                Facets = new[]
                {
                    new Facet{ Name = "testResult", FacetValues = new[] {
                        new FacetValue { Count = 10, Name = "Passed" },
                        new FacetValue { Count = 87, Name = "Failed" },
                        new FacetValue { Count = 6, Name = "Ignored" }
                    } }
                }
            };

            ITestResultsSearchService searchService = CreatetestResultsSearchService();
            searchService
                .SearchTestScenarioResults(Arg.Any<SearchModel>())
                .Returns(testScenarioSearchResults1);

            TestResultsCountsService service = CreateResultCountsService(searchService, logger);

            //Act
            IActionResult result = await service.GetResultCounts(httpRequest);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();

            OkObjectResult okResult = result as OkObjectResult;

            IList<TestScenarioResultCounts> results = okResult.Value as IList<TestScenarioResultCounts>;

            results
                .Count
                .Should()
                .Be(1);

            results
                .First()
                .Passed
                .Should()
                .Be(10);

            results
                .First()
                .Failed
                .Should()
                .Be(87);

            results
                .First()
                .Ignored
                .Should()
                .Be(6);
        }

        static TestResultsCountsService CreateResultCountsService(ITestResultsSearchService testResultsService = null, ILogger logger = null)
        {
            return new TestResultsCountsService(testResultsService ?? CreatetestResultsSearchService(), logger ?? CreateLogger());
        }

        static ITestResultsSearchService CreatetestResultsSearchService()
        {
            return Substitute.For<ITestResultsSearchService>();
        }

        static ILogger CreateLogger()
        {
            return Substitute.For<ILogger>();
        }
    }
}
