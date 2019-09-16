using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CalculateFunding.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Repositories.Common.Search;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Search.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using NSubstitute;
using Serilog;

namespace CalculateFunding.Services.Publishing.UnitTests
{
    public partial class PublishedSearchServiceTests
    {
        [TestMethod]
        public async Task SearchPublishedProviders_GivenNullSearchModel_ReturnsBadRequest()
        {
            //Arrange
            HttpRequest request = Substitute.For<HttpRequest>();

            ILogger logger = CreateLogger();

            PublishedSearchService service = CreateSearchService(logger: logger);

            //Act
            IActionResult result = await service.SearchPublishedProviders(request);

            //Assert
            logger
                .Received(1)
                .Error("A null or invalid search model was provided for searching published providers");

            result
                .Should()
                .BeOfType<BadRequestObjectResult>();
        }

        [TestMethod]
        public async Task SearchPublishedProviders_GivenInvalidPageNumber_ReturnsBadRequest()
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

            PublishedSearchService service = CreateSearchService(logger: logger);

            //Act
            IActionResult result = await service.SearchPublishedProviders(request);

            //Assert
            logger
                .Received(1)
                .Error("A null or invalid search model was provided for searching published providers");

            result
                .Should()
                .BeOfType<BadRequestObjectResult>();
        }

        [TestMethod]
        public async Task SearchPublishedProviders_GivenInvalidTop_ReturnsBadRequest()
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

            PublishedSearchService service = CreateSearchService(logger: logger);

            //Act
            IActionResult result = await service.SearchPublishedProviders(request);

            //Assert
            logger
                .Received(1)
                .Error("A null or invalid search model was provided for searching published providers");

            result
                .Should()
                .BeOfType<BadRequestObjectResult>();
        }

        [TestMethod]
        public async Task SearchPublishedProviders_GivenValidModelAndIncludesGettingFacets_CallsSearchFourTimes()
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

            SearchResults<PublishedProviderIndex> searchResults = new SearchResults<PublishedProviderIndex>();

            ILogger logger = CreateLogger();

            ISearchRepository<PublishedProviderIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .Search(Arg.Any<string>(), Arg.Any<SearchParameters>())
                .Returns(searchResults);

            PublishedSearchService service = CreateSearchService(logger: logger, searchRepository: searchRepository);

            //Act
            IActionResult result = await service.SearchPublishedProviders(request);

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
        public async Task SearchPublishedProviders_GivenValidModelWithNullFilters_ThenSearchIsStillPerformed()
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

            SearchResults<PublishedProviderIndex> searchResults = new SearchResults<PublishedProviderIndex>();

            ILogger logger = CreateLogger();

            ISearchRepository<PublishedProviderIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .Search(Arg.Any<string>(), Arg.Any<SearchParameters>())
                .Returns(searchResults);

            PublishedSearchService service = CreateSearchService(logger: logger, searchRepository: searchRepository);

            //Act
            IActionResult result = await service.SearchPublishedProviders(request);

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
        public async Task SearchPublishedProviders_GivenValidModelWithOneFilter_ThenSearchIsPerformed()
        {
            //Arrange
            SearchModel model = new SearchModel
            {
                PageNumber = 1,
                Top = 50,
                IncludeFacets = true,
                Filters = new Dictionary<string, string[]>()
                {
                    { "providerType", new string []{ "test" } }
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

            SearchResults<PublishedProviderIndex> searchResults = new SearchResults<PublishedProviderIndex>();

            ILogger logger = CreateLogger();

            ISearchRepository<PublishedProviderIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .Search(Arg.Any<string>(), Arg.Any<SearchParameters>())
                .Returns(searchResults);

            PublishedSearchService service = CreateSearchService(logger: logger, searchRepository: searchRepository);

            //Act
            IActionResult result = await service.SearchPublishedProviders(request);

            //Assert
            result
                 .Should()
                 .BeOfType<OkObjectResult>();

            await
                searchRepository
                .Received(3)
                    .Search(model.SearchTerm, Arg.Is<SearchParameters>(c =>
                        model.Filters.Keys.All(f => c.Filter.Contains(f))
                        && !string.IsNullOrWhiteSpace(c.Filter)
                    ));
        }

        [TestMethod]
        public async Task SearchPublishedProviders_GivenValidModelWithOneFilterWhichIsMultiValueFacet_ThenSearchIsPerformed()
        {
            //Arrange
            SearchModel model = new SearchModel
            {
                PageNumber = 1,
                Top = 50,
                IncludeFacets = true,
                Filters = new Dictionary<string, string[]>()
                {
                    { "providerType", new string []{ "test" } }
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

            SearchResults<PublishedProviderIndex> searchResults = new SearchResults<PublishedProviderIndex>();

            ILogger logger = CreateLogger();

            ISearchRepository<PublishedProviderIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .Search(Arg.Any<string>(), Arg.Any<SearchParameters>())
                .Returns(searchResults);

            PublishedSearchService service = CreateSearchService(logger: logger, searchRepository: searchRepository);

            //Act
            IActionResult result = await service.SearchPublishedProviders(request);

            //Assert
            result
                 .Should()
                 .BeOfType<OkObjectResult>();

            await
                searchRepository
                .Received(3)
                    .Search(model.SearchTerm, Arg.Is<SearchParameters>(c =>
                        model.Filters.Keys.All(f => c.Filter.Contains(f))
                        && !string.IsNullOrWhiteSpace(c.Filter)
                    ));
        }

        [TestMethod]
        public async Task SearchPublishedProviders_GivenValidModelWithMultipleFilterValuesWhichIsMultiValueFacet_ThenSearchIsPerformed()
        {
            //Arrange
            SearchModel model = new SearchModel
            {
                PageNumber = 1,
                Top = 50,
                IncludeFacets = true,
                Filters = new Dictionary<string, string[]>()
                {
                    { "providerType", new string []{ "test", "test2" } }
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

            SearchResults<PublishedProviderIndex> searchResults = new SearchResults<PublishedProviderIndex>();

            ILogger logger = CreateLogger();

            ISearchRepository<PublishedProviderIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .Search(Arg.Any<string>(), Arg.Any<SearchParameters>())
                .Returns(searchResults);

            PublishedSearchService service = CreateSearchService(logger: logger, searchRepository: searchRepository);

            //Act
            IActionResult result = await service.SearchPublishedProviders(request);

            //Assert
            result
                 .Should()
                 .BeOfType<OkObjectResult>();

            await
                searchRepository
                .Received(3)
                    .Search(model.SearchTerm, Arg.Is<SearchParameters>(c =>
                        model.Filters.Keys.All(f => c.Filter.Contains(f))
                        && !string.IsNullOrWhiteSpace(c.Filter)
                    ));
        }

        [TestMethod]
        public async Task SearchPublishedProviders_GivenValidModelWithMultipleOfSameFilter_ThenSearchIsPerformed()
        {
            //Arrange
            SearchModel model = new SearchModel
            {
                PageNumber = 1,
                Top = 50,
                IncludeFacets = true,
                Filters = new Dictionary<string, string[]>()
                {
                    { "providerType", new string []{ "test", "test2" } }
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

            SearchResults<PublishedProviderIndex> searchResults = new SearchResults<PublishedProviderIndex>();

            ILogger logger = CreateLogger();

            ISearchRepository<PublishedProviderIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .Search(Arg.Any<string>(), Arg.Any<SearchParameters>())
                .Returns(searchResults);

            PublishedSearchService service = CreateSearchService(logger: logger, searchRepository: searchRepository);

            //Act
            IActionResult result = await service.SearchPublishedProviders(request);

            //Assert
            result
                 .Should()
                 .BeOfType<OkObjectResult>();

            await
                searchRepository
                .Received(3)
                    .Search(model.SearchTerm, Arg.Is<SearchParameters>(c =>
                        model.Filters.Keys.All(f => c.Filter.Contains(f))
                        && !string.IsNullOrWhiteSpace(c.Filter)
                    ));
        }

        [TestMethod]
        public async Task SearchPublishedProviders_GivenValidModelWithNullFilterWithMultipleOfSameFilter_ThenSearchIsPerformed()
        {
            //Arrange
            SearchModel model = new SearchModel
            {
                PageNumber = 1,
                Top = 50,
                IncludeFacets = true,
                Filters = new Dictionary<string, string[]>()
                {
                    { "providerType", new string []{ "test", "" } }
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

            SearchResults<PublishedProviderIndex> searchResults = new SearchResults<PublishedProviderIndex>();

            ILogger logger = CreateLogger();

            ISearchRepository<PublishedProviderIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .Search(Arg.Any<string>(), Arg.Any<SearchParameters>())
                .Returns(searchResults);

            PublishedSearchService service = CreateSearchService(logger: logger, searchRepository: searchRepository);

            //Act
            IActionResult result = await service.SearchPublishedProviders(request);

            //Assert
            result
                 .Should()
                 .BeOfType<OkObjectResult>();

            await
                searchRepository
                .Received(3)
                    .Search(model.SearchTerm, Arg.Is<SearchParameters>(c =>
                        model.Filters.Keys.All(f => c.Filter.Contains(f))
                        && !string.IsNullOrWhiteSpace(c.Filter)
                    ));
        }

        [TestMethod]
        public async Task SearchPublishedProviders_GivenValidModelAndDoesntIncludeGettingFacets_CallsSearchOnce()
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

            SearchResults<PublishedProviderIndex> searchResults = new SearchResults<PublishedProviderIndex>();

            ILogger logger = CreateLogger();

            ISearchRepository<PublishedProviderIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .Search(Arg.Any<string>(), Arg.Any<SearchParameters>())
                .Returns(searchResults);

            PublishedSearchService service = CreateSearchService(logger: logger, searchRepository: searchRepository);

            //Act
            IActionResult result = await service.SearchPublishedProviders(request);

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
        public async Task SearchPublishedProviders_GivenValidModelAndPageNumber2_CallsSearchWithCorrectSkipValue()
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

            SearchResults<PublishedProviderIndex> searchResults = new SearchResults<PublishedProviderIndex>();

            ILogger logger = CreateLogger();

            ISearchRepository<PublishedProviderIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .Search(Arg.Any<string>(), Arg.Any<SearchParameters>())
                .Returns(searchResults);

            PublishedSearchService service = CreateSearchService(logger: logger, searchRepository: searchRepository);

            //Act
            IActionResult result = await service.SearchPublishedProviders(request);

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
        public async Task SearchPublishedProviders_GivenValidModelAndPageNumber10_CallsSearchWithCorrectSkipValue()
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

            SearchResults<PublishedProviderIndex> searchResults = new SearchResults<PublishedProviderIndex>();

            ILogger logger = CreateLogger();

            ISearchRepository<PublishedProviderIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .Search(Arg.Any<string>(), Arg.Any<SearchParameters>())
                .Returns(searchResults);

            PublishedSearchService service = CreateSearchService(logger: logger, searchRepository: searchRepository);

            //Act
            IActionResult result = await service.SearchPublishedProviders(request);

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
        public async Task SearchPublishedProviders_GivenValidModel_CallsSearchWithCorrectSkipValue()
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

            SearchResults<PublishedProviderIndex> searchResults = new SearchResults<PublishedProviderIndex>
            {
                Facets = new List<Facet>
                {
                    new Facet
                    {
                        Name = "fundingPeriodName"
                    }
                }
            };

            ILogger logger = CreateLogger();

            ISearchRepository<PublishedProviderIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .Search(Arg.Any<string>(), Arg.Any<SearchParameters>())
                .Returns(searchResults);

            PublishedSearchService service = CreateSearchService(logger: logger, searchRepository: searchRepository);

            //Act
            IActionResult result = await service.SearchPublishedProviders(request);

            //Assert
            result
                 .Should()
                 .BeOfType<OkObjectResult>();

            await
                searchRepository
                    .Received(4)
                    .Search(Arg.Any<string>(), Arg.Any<SearchParameters>());
        }
    }
}
