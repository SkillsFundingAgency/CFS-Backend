using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
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
    public class ResultsSearchServiceTests
    {
        [TestMethod]
        public async Task SearchResult_SearchRequestFails_ThenBadRequestReturned()
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

            ISearchRepository<ProviderIndex> searchRepository = CreateSearchRepository();

            searchRepository
                .When(s => s.Search(Arg.Any<string>(), Arg.Any<SearchParameters>()))
                    .Do(x => { throw new FailedToQuerySearchException("Test Message", null); });


            ResultsSearchService service = CreateResultsSearchService(logger: logger, searchRepository: searchRepository);

            //Act
            IActionResult result = await service.SearchProviders(request);

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
        public async Task SearchResult_GivenNullSearchModel_LogsAndCreatesDefaultSearchModel()
        {
            //Arrange
            HttpRequest request = Substitute.For<HttpRequest>();

            ILogger logger = CreateLogger();

            ISearchRepository<ProviderIndex> searchRepository = CreateSearchRepository();

            ResultsSearchService service = CreateResultsSearchService(logger: logger, searchRepository: searchRepository);

            //Act
            IActionResult result = await service.SearchProviders(request);

            //Assert
            logger
                .Received(1)
                .Error("A null or invalid search model was provided for searching providers");

            result
                 .Should()
                 .BeOfType<BadRequestObjectResult>();
        }

        [TestMethod]
        public async Task SearchResult_GivenPageNumberIsZero_LogsAndReturnsBadRequest()
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

            ISearchRepository<ProviderIndex> searchRepository = CreateSearchRepository();

            ResultsSearchService service = CreateResultsSearchService(logger: logger, searchRepository: searchRepository);

            //Act
            IActionResult result = await service.SearchProviders(request);

            //Assert
            logger
                .Received(1)
                .Error("A null or invalid search model was provided for searching providers");

            result
                 .Should()
                 .BeOfType<BadRequestObjectResult>();
        }

        [TestMethod]
        public async Task SearchResult_GivenPageTopIsZero_LogsAndReturnsBadRequest()
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

            ISearchRepository<ProviderIndex> searchRepository = CreateSearchRepository();

            ResultsSearchService service = CreateResultsSearchService(logger: logger, searchRepository: searchRepository);

            //Act
            IActionResult result = await service.SearchProviders(request);

            //Assert
            logger
                .Received(1)
                .Error("A null or invalid search model was provided for searching providers");

            result
                 .Should()
                 .BeOfType<BadRequestObjectResult>();
        }

        [TestMethod]
        public async Task SearchResult_GivenValidModelAndIncludesGettingFacets_CallsSearchFiveTimes()
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

            SearchResults<ProviderIndex> SearchProviders = new SearchResults<ProviderIndex>();

            ILogger logger = CreateLogger();

            ISearchRepository<ProviderIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .Search(Arg.Any<string>(), Arg.Any<SearchParameters>())
                .Returns(SearchProviders);

            ResultsSearchService service = CreateResultsSearchService(logger: logger, searchRepository: searchRepository);

            //Act
            IActionResult result = await service.SearchProviders(request);

            //Assert
            result
                 .Should()
                 .BeOfType<OkObjectResult>();

            await
                searchRepository
                    .Received(4)
                    .Search(Arg.Any<string>(), Arg.Any<SearchParameters>());

        }

        [TestMethod]
        public async Task SearchResult_GivenValidModelWithNullFilters_ThenSearchIsStillPerformed()
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

            SearchResults<ProviderIndex> SearchProviders = new SearchResults<ProviderIndex>();

            ILogger logger = CreateLogger();

            ISearchRepository<ProviderIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .Search(Arg.Any<string>(), Arg.Any<SearchParameters>())
                .Returns(SearchProviders);

            ResultsSearchService service = CreateResultsSearchService(logger: logger, searchRepository: searchRepository);

            //Act
            IActionResult result = await service.SearchProviders(request);

            //Assert
            result
                 .Should()
                 .BeOfType<OkObjectResult>();

            await
                searchRepository
                    .Received(4)
                    .Search(Arg.Any<string>(), Arg.Any<SearchParameters>());

        }

        [TestMethod]
        public async Task SearchResult_GivenValidModelWithOneFilter_ThenSearchIsPerformed()
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

            SearchResults<ProviderIndex> SearchProviders = new SearchResults<ProviderIndex>();

            ILogger logger = CreateLogger();

            ISearchRepository<ProviderIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .Search(Arg.Any<string>(), Arg.Any<SearchParameters>())
                .Returns(SearchProviders);

            ResultsSearchService service = CreateResultsSearchService(logger: logger, searchRepository: searchRepository);

            //Act
            IActionResult result = await service.SearchProviders(request);

            //Assert
            result
                 .Should()
                 .BeOfType<OkObjectResult>();

            await
                searchRepository
                .Received(4)
                    .Search(model.SearchTerm, Arg.Any<SearchParameters>());
        }

        [TestMethod]
        public async Task SearchResult_GivenValidModelWithOneFilterWhichIsMultiValueFacet_ThenSearchIsPerformed()
        {
            //Arrange
            SearchModel model = new SearchModel
            {
                PageNumber = 1,
                Top = 50,
                IncludeFacets = true,
                Filters = new Dictionary<string, string[]>()
                {
                    { "specificationNames", new string []{ "test" } }
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

            SearchResults<ProviderIndex> SearchProviders = new SearchResults<ProviderIndex>();

            ILogger logger = CreateLogger();

            ISearchRepository<ProviderIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .Search(Arg.Any<string>(), Arg.Any<SearchParameters>())
                .Returns(SearchProviders);

            ResultsSearchService service = CreateResultsSearchService(logger: logger, searchRepository: searchRepository);

            //Act
            IActionResult result = await service.SearchProviders(request);

            //Assert
            result
                 .Should()
                 .BeOfType<OkObjectResult>();

            await
                searchRepository
                .Received(4)
                    .Search(model.SearchTerm, Arg.Any<SearchParameters>());
        }

        [TestMethod]
        public async Task SearchResult_GivenValidModelWithMultipleFilterValuesWhichIsMultiValueFacet_ThenSearchIsPerformed()
        {
            //Arrange
            SearchModel model = new SearchModel
            {
                PageNumber = 1,
                Top = 50,
                IncludeFacets = true,
                Filters = new Dictionary<string, string[]>()
                {
                    { "specificationNames", new string []{ "test", "test2" } }
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

            SearchResults<ProviderIndex> SearchProviders = new SearchResults<ProviderIndex>();

            ILogger logger = CreateLogger();

            ISearchRepository<ProviderIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .Search(Arg.Any<string>(), Arg.Any<SearchParameters>())
                .Returns(SearchProviders);

            ResultsSearchService service = CreateResultsSearchService(logger: logger, searchRepository: searchRepository);

            //Act
            IActionResult result = await service.SearchProviders(request);

            //Assert
            result
                 .Should()
                 .BeOfType<OkObjectResult>();

            await
                 searchRepository
                 .Received(4)
                     .Search(model.SearchTerm, Arg.Any<SearchParameters>());
        }

        [TestMethod]
        public async Task SearchResult_GivenValidModelWithMultipleOfSameFilter_ThenSearchIsPerformed()
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

            SearchResults<ProviderIndex> SearchProviders = new SearchResults<ProviderIndex>();

            ILogger logger = CreateLogger();

            ISearchRepository<ProviderIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .Search(Arg.Any<string>(), Arg.Any<SearchParameters>())
                .Returns(SearchProviders);

            ResultsSearchService service = CreateResultsSearchService(logger: logger, searchRepository: searchRepository);

            //Act
            IActionResult result = await service.SearchProviders(request);

            //Assert
            result
                 .Should()
                 .BeOfType<OkObjectResult>();

            await
                searchRepository
                .Received(4)
                    .Search(model.SearchTerm, Arg.Any<SearchParameters>());
        }

        [TestMethod]
        public async Task SearchResult_GivenValidModelWithNullFilterWithMultipleOfSameFilter_ThenSearchIsPerformed()
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

            SearchResults<ProviderIndex> SearchProviders = new SearchResults<ProviderIndex>();

            ILogger logger = CreateLogger();

            ISearchRepository<ProviderIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .Search(Arg.Any<string>(), Arg.Any<SearchParameters>())
                .Returns(SearchProviders);

            ResultsSearchService service = CreateResultsSearchService(logger: logger, searchRepository: searchRepository);

            //Act
            IActionResult result = await service.SearchProviders(request);

            //Assert
            result
                 .Should()
                 .BeOfType<OkObjectResult>();

            await
                searchRepository
                .Received(4)
                    .Search(model.SearchTerm, Arg.Any<SearchParameters>());
        }

        [TestMethod]
        public async Task SearchResult_GivenValidModelAndDoesntIncludeGettingFacets_CallsSearchOnce()
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

            SearchResults<ProviderIndex> SearchProviders = new SearchResults<ProviderIndex>();

            ILogger logger = CreateLogger();

            ISearchRepository<ProviderIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .Search(Arg.Any<string>(), Arg.Any<SearchParameters>())
                .Returns(SearchProviders);

            ResultsSearchService service = CreateResultsSearchService(logger: logger, searchRepository: searchRepository);

            //Act
            IActionResult result = await service.SearchProviders(request);

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
        public async Task SearchResult_GivenValidModelAndPageNumber2_CallsSearchWithCorrectSkipValue()
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

            SearchResults<ProviderIndex> SearchProviders = new SearchResults<ProviderIndex>();

            ILogger logger = CreateLogger();

            ISearchRepository<ProviderIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .Search(Arg.Any<string>(), Arg.Any<SearchParameters>())
                .Returns(SearchProviders);

            ResultsSearchService service = CreateResultsSearchService(logger: logger, searchRepository: searchRepository);

            //Act
            IActionResult result = await service.SearchProviders(request);

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
        public async Task SearchResult_GivenValidModelAndPageNumber10_CallsSearchWithCorrectSkipValue()
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

            SearchResults<ProviderIndex> SearchProviders = new SearchResults<ProviderIndex>();

            ILogger logger = CreateLogger();

            ISearchRepository<ProviderIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .Search(Arg.Any<string>(), Arg.Any<SearchParameters>())
                .Returns(SearchProviders);

            ResultsSearchService service = CreateResultsSearchService(logger: logger, searchRepository: searchRepository);

            //Act
            IActionResult result = await service.SearchProviders(request);

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
        public async Task SearchResult_GivenValidModel_CallsSearchWithCorrectSkipValue()
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

            SearchResults<ProviderIndex> SearchProviders = new SearchResults<ProviderIndex>
            {
                Facets = new List<Facet>
                {
                    new Facet
                    {
                        Name = "authority"
                    }
                }
            };

            ILogger logger = CreateLogger();

            ISearchRepository<ProviderIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .Search(Arg.Any<string>(), Arg.Any<SearchParameters>())
                .Returns(SearchProviders);

            ResultsSearchService service = CreateResultsSearchService(logger: logger, searchRepository: searchRepository);

            //Act
            IActionResult result = await service.SearchProviders(request);

            //Assert
            result
                 .Should()
                 .BeOfType<OkObjectResult>();

            await
                searchRepository
                .Received(4)
                    .Search(model.SearchTerm, Arg.Any<SearchParameters>());
        }


        static ResultsSearchService CreateResultsSearchService(
            ILogger logger = null,
            ISearchRepository<ProviderIndex> searchRepository = null,
            IResultsResiliencePolicies resiliencePolicies = null)
        {
            return new ResultsSearchService(
                logger ?? CreateLogger(),
                searchRepository ?? CreateSearchRepository(),
                resiliencePolicies ?? ResultsResilienceTestHelper.GenerateTestPolicies());
        }

        static ILogger CreateLogger()
        {
            return Substitute.For<ILogger>();
        }

        static ISearchRepository<ProviderIndex> CreateSearchRepository()
        {
            return Substitute.For<ISearchRepository<ProviderIndex>>();
        }
    }
}
