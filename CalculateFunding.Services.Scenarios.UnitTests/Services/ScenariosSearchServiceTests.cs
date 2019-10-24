using CalculateFunding.Common.ApiClient.Specifications;
using CalculateFunding.Models;
using CalculateFunding.Models.Scenarios;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Scenarios.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Search.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using NSubstitute;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Scenarios.Services
{
    [TestClass]
    public class ScenariosSearchServiceTests
    {
        [TestMethod]
        public async Task SearchSpecifications_GivenNullSearchModel_ReturnsBadRequest()
        {
            //Arrange
            HttpRequest request = Substitute.For<HttpRequest>();

            ILogger logger = CreateLogger();

            ScenariosSearchService service = CreateSearchService(logger: logger);

            //Act
            IActionResult result = await service.SearchScenarios(request);

            //Assert
            logger
                .Received(1)
                .Error("A null or invalid search model was provided for searching scenarios");

            result
                .Should()
                .BeOfType<BadRequestObjectResult>();
        }

        [TestMethod]
        public async Task SearchScenarios_GivenInvalidPageNumber_ReturnsBadRequest()
        {
            //Arrange
            SearchModel model = new SearchModel
            {
                PageNumber = 0,
                Top = 1
            };
            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Body
                .Returns(stream);

            ILogger logger = CreateLogger();

            ScenariosSearchService service = CreateSearchService(logger: logger);

            //Act
            IActionResult result = await service.SearchScenarios(request);

            //Assert
            logger
                .Received(1)
                .Error("A null or invalid search model was provided for searching scenarios");

            result
                .Should()
                .BeOfType<BadRequestObjectResult>();
        }

        [TestMethod]
        public async Task SearchScenarios_GivenInvalidTop_ReturnsBadRequest()
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

            ScenariosSearchService service = CreateSearchService(logger: logger);

            //Act
            IActionResult result = await service.SearchScenarios(request);

            //Assert
            logger
                .Received(1)
                .Error("A null or invalid search model was provided for searching scenarios");

            result
                .Should()
                .BeOfType<BadRequestObjectResult>();
        }

        [TestMethod]
        async public Task SearchScenarios_GivenSearchThrowsException_ReturnsStatusCode500()
        {
            //Arrange
            SearchModel model = CreateSearchModel();
            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Body
                .Returns(stream);

            ISearchRepository<ScenarioIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .When(x => x.Search(Arg.Any<string>(), Arg.Any<SearchParameters>()))
                .Do(x => { throw new FailedToQuerySearchException("main", new Exception("inner")); });

            ILogger logger = CreateLogger();

            ScenariosSearchService service = CreateSearchService(searchRepository, logger: logger);

            //Act
            IActionResult result = await service.SearchScenarios(request);

            //Assert
            StatusCodeResult statusCodeResult = result
                .Should()
                .BeOfType<StatusCodeResult>()
                .Subject;

            statusCodeResult
                .StatusCode
                .Should()
                .Be(500);
        }

        [TestMethod]
        async public Task SearchScenarios_GivenSearchReturnsResults_ReturnsOKResult()
        {
            //Arrange
            SearchModel model = CreateSearchModel();
            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Body
                .Returns(stream);

            SearchResults<ScenarioIndex> searchResults = new SearchResults<ScenarioIndex>
            {
                TotalCount = 1
            };

            ISearchRepository<ScenarioIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .Search(Arg.Is("SearchTermTest"), Arg.Any<SearchParameters>())
                .Returns(searchResults);

            ILogger logger = CreateLogger();

            ScenariosSearchService service = CreateSearchService(searchRepository, logger: logger);

            //Act
            IActionResult result = await service.SearchScenarios(request);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();
        }

        [TestMethod]
        async public Task SearchScenarios_GivenSearchReturnsResultsAndDataLastUpdateIsNull_ReturnsOKResult()
        {
            //Arrange
            SearchModel model = CreateSearchModel();
            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Body
                .Returns(stream);

            SearchResults<ScenarioIndex> searchResults = new SearchResults<ScenarioIndex>
            {
                TotalCount = 1,
                Results = new List<Repositories.Common.Search.SearchResult<ScenarioIndex>>
                {
                    new Repositories.Common.Search.SearchResult<ScenarioIndex>
                    {
                        Result = new ScenarioIndex
                        {
                            LastUpdatedDate = null
                        }
                    }
                }
            };

            ISearchRepository<ScenarioIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .Search(Arg.Is("SearchTermTest"), Arg.Any<SearchParameters>())
                .Returns(searchResults);

            ILogger logger = CreateLogger();

            ScenariosSearchService service = CreateSearchService(searchRepository, logger: logger);

            //Act
            IActionResult result = await service.SearchScenarios(request);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();

            OkObjectResult okObjectResult = result as OkObjectResult;

        }

        static ScenariosSearchService CreateSearchService(
            ISearchRepository<ScenarioIndex> searchRepository = null,
            IScenariosRepository scenariosRepository = null,
            ISpecificationsApiClient specificationsApiClient = null,
            ILogger logger = null,
            IScenariosResiliencePolicies scenariosResiliencePolicies = null)
        {
            return new ScenariosSearchService(
                searchRepository ?? CreateSearchRepository(), 
                scenariosRepository ?? CreateScenariosRepository(),
                 specificationsApiClient ?? CreateSpecificationsApiClient(),
                logger ?? CreateLogger(),
                scenariosResiliencePolicies ?? ScenariosResilienceTestHelper.GenerateTestPolicies());
        }

        static ISearchRepository<ScenarioIndex> CreateSearchRepository()
        {
            return Substitute.For<ISearchRepository<ScenarioIndex>>();
        }

        static IScenariosRepository CreateScenariosRepository()
        {
            return Substitute.For<IScenariosRepository>();
        }

        static ISpecificationsApiClient CreateSpecificationsApiClient()
        {
            return Substitute.For<ISpecificationsApiClient>();
        }

        static ILogger CreateLogger()
        {
            return Substitute.For<ILogger>();
        }

        static SearchModel CreateSearchModel()
        {
            return new SearchModel()
            {
                SearchTerm = "SearchTermTest",
                PageNumber = 1,
                Top = 20,
                Filters = new Dictionary<string, string[]>
                {
                    { "periodName" , new[]{"18/19" } },
                    { "specificationName", new[]{"test spec" } }
                }
            };
        }
    }
}
