using CalculateFunding.Models;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Repositories.Common.Search;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Search.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using NSubstitute;
using Serilog;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Calcs.Services
{
    [TestClass]
    public class CalculationSearchServiceTests
    {
        [TestMethod]
        public async Task SearchCalculation_SearchRequestFails_ThenBadRequestReturned()
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

            ISearchRepository<CalculationIndex> searchRepository = CreateSearchRepository();

            searchRepository
                .When(s => s.Search(Arg.Any<string>(), Arg.Any<SearchParameters>()))
                    .Do(x => { throw new FailedToQuerySearchException("Test Message", null); });


            CalculationSearchService service = CreateCalculationSearchService(logger: logger, serachRepository: searchRepository);

            //Act
            IActionResult result = await service.SearchCalculations(request);

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
        public async Task SearchCalculation_GivenNullSearchModel_LogsAndCreatesDefaultSearcModel()
        {
            //Arrange
            HttpRequest request = Substitute.For<HttpRequest>();

            ILogger logger = CreateLogger();

            ISearchRepository<CalculationIndex> searchRepository = CreateSearchRepository();

            CalculationSearchService service = CreateCalculationSearchService(logger: logger, serachRepository: searchRepository);

            //Act
            IActionResult result = await service.SearchCalculations(request);

            //Assert
            logger
                .Received(1)
                .Error("A null or invalid search model was provided for searching calculations");

            result
                 .Should()
                 .BeOfType<BadRequestObjectResult>();
        }

        [TestMethod]
        public async Task SearchCalculation_GivenPageNumberIsZero_LogsAndReturnsBadRequest()
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

            ISearchRepository<CalculationIndex> searchRepository = CreateSearchRepository();

            CalculationSearchService service = CreateCalculationSearchService(logger: logger, serachRepository: searchRepository);

            //Act
            IActionResult result = await service.SearchCalculations(request);

            //Assert
            logger
                .Received(1)
                .Error("A null or invalid search model was provided for searching calculations");

            result
                 .Should()
                 .BeOfType<BadRequestObjectResult>();
        }

        [TestMethod]
        public async Task SearchCalculation_GivenPageTopIsZero_LogsAndReturnsBadRequest()
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

            ISearchRepository<CalculationIndex> searchRepository = CreateSearchRepository();

            CalculationSearchService service = CreateCalculationSearchService(logger: logger, serachRepository: searchRepository);

            //Act
            IActionResult result = await service.SearchCalculations(request);

            //Assert
            logger
                .Received(1)
                .Error("A null or invalid search model was provided for searching calculations");

            result
                 .Should()
                 .BeOfType<BadRequestObjectResult>();
        }

        [TestMethod]
        public async Task SearchCalculation_GivenValidModelAndIncludesGettingFacets_CallsSearchSevenTimes()
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

            SearchResults<CalculationIndex> searchResults = new SearchResults<CalculationIndex>();

            ILogger logger = CreateLogger();

            ISearchRepository<CalculationIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .Search(Arg.Any<string>(), Arg.Any<SearchParameters>())
                .Returns(searchResults);

            CalculationSearchService service = CreateCalculationSearchService(logger: logger, serachRepository: searchRepository);

            //Act
            IActionResult result = await service.SearchCalculations(request);

            //Assert
            result
                 .Should()
                 .BeOfType<OkObjectResult>();

            await
                searchRepository
                    .Received(7)
                    .Search(Arg.Any<string>(), Arg.Any<SearchParameters>());

        }

        [TestMethod]
        public async Task SearchCalculation_GivenValidModelWithNullFilters_ThenSearchIsStillPerformed()
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

            SearchResults<CalculationIndex> searchResults = new SearchResults<CalculationIndex>();

            ILogger logger = CreateLogger();

            ISearchRepository<CalculationIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .Search(Arg.Any<string>(), Arg.Any<SearchParameters>())
                .Returns(searchResults);

            CalculationSearchService service = CreateCalculationSearchService(logger: logger, serachRepository: searchRepository);

            //Act
            IActionResult result = await service.SearchCalculations(request);

            //Assert
            result
                 .Should()
                 .BeOfType<OkObjectResult>();

            await
                searchRepository
                    .Received(7)
                    .Search(Arg.Any<string>(), Arg.Any<SearchParameters>());

        }

        [TestMethod]
        public async Task SearchCalculation_GivenValidModelWithOneFilter_ThenSearchIsPerformed()
        {
            //Arrange
            SearchModel model = new SearchModel
            {
                PageNumber = 1,
                Top = 50,
                IncludeFacets = true,
                Filters = new Dictionary<string, string[]>()
                {
                    { "status", new string []{ "test" } }
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

            SearchResults<CalculationIndex> searchResults = new SearchResults<CalculationIndex>();

            ILogger logger = CreateLogger();

            ISearchRepository<CalculationIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .Search(Arg.Any<string>(), Arg.Any<SearchParameters>())
                .Returns(searchResults);

            CalculationSearchService service = CreateCalculationSearchService(logger: logger, serachRepository: searchRepository);

            //Act
            IActionResult result = await service.SearchCalculations(request);

            //Assert
            result
                 .Should()
                 .BeOfType<OkObjectResult>();

            await
                searchRepository
                .Received(6)
                    .Search(model.SearchTerm, Arg.Is<SearchParameters>(c =>
                        model.Filters.Keys.All(f => c.Filter.Contains(f))
                        && !string.IsNullOrWhiteSpace(c.Filter)
                    ));
        }

        [TestMethod]
        public async Task SearchCalculation_GivenValidModelWithOneFilterWhichIsMultiValueFacet_ThenSearchIsPerformed()
        {
            //Arrange
            SearchModel model = new SearchModel
            {
                PageNumber = 1,
                Top = 50,
                IncludeFacets = true,
                Filters = new Dictionary<string, string[]>()
                {
                    { "policySpecificationNames", new string []{ "test" } }
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

            SearchResults<CalculationIndex> searchResults = new SearchResults<CalculationIndex>();

            ILogger logger = CreateLogger();

            ISearchRepository<CalculationIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .Search(Arg.Any<string>(), Arg.Any<SearchParameters>())
                .Returns(searchResults);

            CalculationSearchService service = CreateCalculationSearchService(logger: logger, serachRepository: searchRepository);

            //Act
            IActionResult result = await service.SearchCalculations(request);

            //Assert
            result
                 .Should()
                 .BeOfType<OkObjectResult>();

            await
                searchRepository
                .Received(6)
                    .Search(model.SearchTerm, Arg.Is<SearchParameters>(c =>
                        model.Filters.Keys.All(f => c.Filter.Contains(f))
                        && !string.IsNullOrWhiteSpace(c.Filter)
                    ));
        }

        [TestMethod]
        public async Task SearchCalculation_GivenValidModelWithMultipleFilterValuesWhichIsMultiValueFacet_ThenSearchIsPerformed()
        {
            //Arrange
            SearchModel model = new SearchModel
            {
                PageNumber = 1,
                Top = 50,
                IncludeFacets = true,
                Filters = new Dictionary<string, string[]>()
                {
                    { "policySpecificationNames", new string []{ "test", "test2" } }
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

            SearchResults<CalculationIndex> searchResults = new SearchResults<CalculationIndex>();

            ILogger logger = CreateLogger();

            ISearchRepository<CalculationIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .Search(Arg.Any<string>(), Arg.Any<SearchParameters>())
                .Returns(searchResults);

            CalculationSearchService service = CreateCalculationSearchService(logger: logger, serachRepository: searchRepository);

            //Act
            IActionResult result = await service.SearchCalculations(request);

            //Assert
            result
                 .Should()
                 .BeOfType<OkObjectResult>();

            await
                searchRepository
                .Received(6)
                    .Search(model.SearchTerm, Arg.Is<SearchParameters>(c =>
                        model.Filters.Keys.All(f => c.Filter.Contains(f))
                        && !string.IsNullOrWhiteSpace(c.Filter)
                    ));
        }

        [TestMethod]
        public async Task SearchCalculation_GivenValidModelWithMultipleOfSameFilter_ThenSearchIsPerformed()
        {
            //Arrange
            SearchModel model = new SearchModel
            {
                PageNumber = 1,
                Top = 50,
                IncludeFacets = true,
                Filters = new Dictionary<string, string[]>()
                {
                    { "status", new string []{ "test", "test2" } }
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

            SearchResults<CalculationIndex> searchResults = new SearchResults<CalculationIndex>();

            ILogger logger = CreateLogger();

            ISearchRepository<CalculationIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .Search(Arg.Any<string>(), Arg.Any<SearchParameters>())
                .Returns(searchResults);

            CalculationSearchService service = CreateCalculationSearchService(logger: logger, serachRepository: searchRepository);

            //Act
            IActionResult result = await service.SearchCalculations(request);

            //Assert
            result
                 .Should()
                 .BeOfType<OkObjectResult>();

            await
                searchRepository
                .Received(6)
                    .Search(model.SearchTerm, Arg.Is<SearchParameters>(c =>
                        model.Filters.Keys.All(f => c.Filter.Contains(f))
                        && !string.IsNullOrWhiteSpace(c.Filter)
                    ));
        }

        [TestMethod]
        public async Task SearchCalculation_GivenValidModelWithNullFilterWithMultipleOfSameFilter_ThenSearchIsPerformed()
        {
            //Arrange
            SearchModel model = new SearchModel
            {
                PageNumber = 1,
                Top = 50,
                IncludeFacets = true,
                Filters = new Dictionary<string, string[]>()
                {
                    { "status", new string []{ "test", "" } }
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

            SearchResults<CalculationIndex> searchResults = new SearchResults<CalculationIndex>();

            ILogger logger = CreateLogger();

            ISearchRepository<CalculationIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .Search(Arg.Any<string>(), Arg.Any<SearchParameters>())
                .Returns(searchResults);

            CalculationSearchService service = CreateCalculationSearchService(logger: logger, serachRepository: searchRepository);

            //Act
            IActionResult result = await service.SearchCalculations(request);

            //Assert
            result
                 .Should()
                 .BeOfType<OkObjectResult>();

            await
                searchRepository
                .Received(6)
                    .Search(model.SearchTerm, Arg.Is<SearchParameters>(c =>
                        model.Filters.Keys.All(f => c.Filter.Contains(f))
                        && !string.IsNullOrWhiteSpace(c.Filter)
                    ));
        }

        [TestMethod]
        public async Task SearchCalculation_GivenValidModelAndDoesntIncludeGettingFacets_CallsSearchOnce()
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

            SearchResults<CalculationIndex> searchResults = new SearchResults<CalculationIndex>();

            ILogger logger = CreateLogger();

            ISearchRepository<CalculationIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .Search(Arg.Any<string>(), Arg.Any<SearchParameters>())
                .Returns(searchResults);

            CalculationSearchService service = CreateCalculationSearchService(logger: logger, serachRepository: searchRepository);

            //Act
            IActionResult result = await service.SearchCalculations(request);

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
        public async Task SearchCalculation_GivenValidModelAndPageNumber2_CallsSearchWithCorrectSkipValue()
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

            SearchResults<CalculationIndex> searchResults = new SearchResults<CalculationIndex>();

            ILogger logger = CreateLogger();

            ISearchRepository<CalculationIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .Search(Arg.Any<string>(), Arg.Any<SearchParameters>())
                .Returns(searchResults);

            CalculationSearchService service = CreateCalculationSearchService(logger: logger, serachRepository: searchRepository);

            //Act
            IActionResult result = await service.SearchCalculations(request);

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
        public async Task SearchCalculation_GivenValidModelAndPageNumber10_CallsSearchWithCorrectSkipValue()
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

            SearchResults<CalculationIndex> searchResults = new SearchResults<CalculationIndex>();

            ILogger logger = CreateLogger();

            ISearchRepository<CalculationIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .Search(Arg.Any<string>(), Arg.Any<SearchParameters>())
                .Returns(searchResults);

            CalculationSearchService service = CreateCalculationSearchService(logger: logger, serachRepository: searchRepository);

            //Act
            IActionResult result = await service.SearchCalculations(request);

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
        public async Task SearchCalculation_GivenValidModel_CallsSearchWithCorrectSkipValue()
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

            SearchResults<CalculationIndex> searchResults = new SearchResults<CalculationIndex>
            {
                Facets = new List<Facet>
                {
                    new Facet
                    {
                        Name = "allocationLineName"
                    }
                }
            };

            ILogger logger = CreateLogger();

            ISearchRepository<CalculationIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .Search(Arg.Any<string>(), Arg.Any<SearchParameters>())
                .Returns(searchResults);

            CalculationSearchService service = CreateCalculationSearchService(logger: logger, serachRepository: searchRepository);

            //Act
            IActionResult result = await service.SearchCalculations(request);

            //Assert
            result
                 .Should()
                 .BeOfType<OkObjectResult>();

            await
                searchRepository
                    .Received(7)
                    .Search(Arg.Any<string>(), Arg.Any<SearchParameters>());
        }

        static CalculationSearchService CreateCalculationSearchService(
           ILogger logger = null, ISearchRepository<CalculationIndex> serachRepository = null)
        {
            return new CalculationSearchService(
                logger ?? CreateLogger(), serachRepository ?? CreateSearchRepository());
        }

        static ILogger CreateLogger()
        {
            return Substitute.For<ILogger>();
        }

        static ISearchRepository<CalculationIndex> CreateSearchRepository()
        {
            return Substitute.For<ISearchRepository<CalculationIndex>>();
        }
    }
}
