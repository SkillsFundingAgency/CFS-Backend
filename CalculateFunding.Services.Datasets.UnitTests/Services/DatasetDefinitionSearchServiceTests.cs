using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Models;
using CalculateFunding.Models.Datasets.Schema;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Datasets;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Search.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Serilog;

namespace CalculateFunding.Services.Calcs.Services
{
    [TestClass]
    public class DatasetDefinitionSearchServiceTests
    {
        [TestMethod]
        public async Task SearchDatasetDefinition_SearchRequestFails_ThenBadRequestReturned()
        {
            //Arrange
            SearchModel model = new SearchModel()
            {
                SearchTerm = "SearchTermTest",
                PageNumber = 1,
                IncludeFacets = false,
                Top = 50,
            };

            ILogger logger = CreateLogger();

            ISearchRepository<DatasetDefinitionIndex> searchRepository = CreateSearchRepository();

            searchRepository
                .When(s => s.Search(Arg.Any<string>(), Arg.Any<SearchParameters>()))
                    .Do(x => { throw new FailedToQuerySearchException("Test Message", null); });


            DatasetDefinitionSearchService service = CreateDatasetSearchService(logger: logger, searchRepository: searchRepository);

            //Act
            IActionResult result = await service.SearchDatasetDefinitions(model);

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
        public async Task SearchDatasetDefinition_GivenNullSearchModel_LogsAndCreatesDefaultSearchModel()
        {
            //Arrange
            ILogger logger = CreateLogger();

            ISearchRepository<DatasetDefinitionIndex> searchRepository = CreateSearchRepository();

            DatasetDefinitionSearchService service = CreateDatasetSearchService(logger: logger, searchRepository: searchRepository);

            //Act
            IActionResult result = await service.SearchDatasetDefinitions(null);

            //Assert
            logger
                .Received(1)
                .Error("A null or invalid search model was provided for searching datasets");

            result
                 .Should()
                 .BeOfType<BadRequestObjectResult>();
        }

        [TestMethod]
        public async Task SearchDatasetDefinition_GivenPageNumberIsZero_LogsAndReturnsBadRequest()
        {
            //Arrange
            SearchModel model = new SearchModel();

            ILogger logger = CreateLogger();

            ISearchRepository<DatasetDefinitionIndex> searchRepository = CreateSearchRepository();

            DatasetDefinitionSearchService service = CreateDatasetSearchService(logger: logger, searchRepository: searchRepository);

            //Act
            IActionResult result = await service.SearchDatasetDefinitions(model);

            //Assert
            logger
                .Received(1)
                .Error("A null or invalid search model was provided for searching datasets");

            result
                 .Should()
                 .BeOfType<BadRequestObjectResult>();
        }

        [TestMethod]
        public async Task SearchDatasetDefinition_GivenPageTopIsZero_LogsAndReturnsBadRequest()
        {
            //Arrange
            SearchModel model = new SearchModel
            {
                PageNumber = 1,
                Top = 0
            };

            ILogger logger = CreateLogger();

            ISearchRepository<DatasetDefinitionIndex> searchRepository = CreateSearchRepository();

            DatasetDefinitionSearchService service = CreateDatasetSearchService(logger: logger, searchRepository: searchRepository);

            //Act
            IActionResult result = await service.SearchDatasetDefinitions(model);

            //Assert
            logger
                .Received(1)
                .Error("A null or invalid search model was provided for searching datasets");

            result
                 .Should()
                 .BeOfType<BadRequestObjectResult>();
        }

        [TestMethod]
        public async Task SearchDatasetDefinition_GivenValidModelAndIncludesGettingFacets_CallsSearchForAllFacets()
        {
            //Arrange
            SearchModel model = new SearchModel
            {
                PageNumber = 1,
                Top = 50,
                IncludeFacets = true
            };

            SearchResults<DatasetDefinitionIndex> searchResults = new SearchResults<DatasetDefinitionIndex>();

            ILogger logger = CreateLogger();

            ISearchRepository<DatasetDefinitionIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .Search(Arg.Any<string>(), Arg.Any<SearchParameters>())
                .Returns(searchResults);

            DatasetDefinitionSearchService service = CreateDatasetSearchService(logger: logger, searchRepository: searchRepository);

            //Act
            IActionResult result = await service.SearchDatasetDefinitions(model);

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
        public async Task SearchDatasetDefinition_GivenValidModelWithNullFilters_ThenSearchIsStillPerformed()
        {
            //Arrange
            SearchModel model = new SearchModel
            {
                PageNumber = 1,
                Top = 50,
                IncludeFacets = true,
                Filters = null,
            };

            SearchResults<DatasetDefinitionIndex> searchResults = new SearchResults<DatasetDefinitionIndex>();

            ILogger logger = CreateLogger();

            ISearchRepository<DatasetDefinitionIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .Search(Arg.Any<string>(), Arg.Any<SearchParameters>())
                .Returns(searchResults);

            DatasetDefinitionSearchService service = CreateDatasetSearchService(logger: logger, searchRepository: searchRepository);

            //Act
            IActionResult result = await service.SearchDatasetDefinitions(model);

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
        public async Task SearchDatasetDefinition_GivenValidModelWithOneFilter_ThenSearchIsPerformed()
        {
            //Arrange
            SearchModel model = new SearchModel
            {
                PageNumber = 1,
                Top = 50,
                IncludeFacets = true,
                Filters = new Dictionary<string, string[]>()
                {
                    { "providerIdentifier", new string []{ "test" } }
                },
                SearchTerm = "testTerm",
            };

            SearchResults<DatasetDefinitionIndex> searchResults = new SearchResults<DatasetDefinitionIndex>();

            ILogger logger = CreateLogger();

            ISearchRepository<DatasetDefinitionIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .Search(Arg.Any<string>(), Arg.Any<SearchParameters>())
                .Returns(searchResults);

            DatasetDefinitionSearchService service = CreateDatasetSearchService(logger: logger, searchRepository: searchRepository);

            //Act
            IActionResult result = await service.SearchDatasetDefinitions(model);

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
        public async Task SearchDatasetDefinition_GivenValidModelWithOneFilterWhichIsMultiValueFacet_ThenSearchIsPerformed()
        {
            //Arrange
            SearchModel model = new SearchModel
            {
                PageNumber = 1,
                Top = 50,
                IncludeFacets = true,
                Filters = new Dictionary<string, string[]>()
                {
                    { "providerIdentifier", new string []{ "test" } }
                },
                SearchTerm = "testTerm",
            };

            SearchResults<DatasetDefinitionIndex> searchResults = new SearchResults<DatasetDefinitionIndex>();

            ILogger logger = CreateLogger();

            ISearchRepository<DatasetDefinitionIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .Search(Arg.Any<string>(), Arg.Any<SearchParameters>())
                .Returns(searchResults);

            DatasetDefinitionSearchService service = CreateDatasetSearchService(logger: logger, searchRepository: searchRepository);

            //Act
            IActionResult result = await service.SearchDatasetDefinitions(model);

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
        public async Task SearchDatasetDefinition_GivenValidModelWithMultipleFilterValuesWhichIsMultiValueFacet_ThenSearchIsPerformed()
        {
            //Arrange
            SearchModel model = new SearchModel
            {
                PageNumber = 1,
                Top = 50,
                IncludeFacets = true,
                Filters = new Dictionary<string, string[]>()
                {
                    { "providerIdentifier", new string []{ "test", "test2" } }
                },
                SearchTerm = "testTerm",
            };

            SearchResults<DatasetDefinitionIndex> searchResults = new SearchResults<DatasetDefinitionIndex>();

            ILogger logger = CreateLogger();

            ISearchRepository<DatasetDefinitionIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .Search(Arg.Any<string>(), Arg.Any<SearchParameters>())
                .Returns(searchResults);

            DatasetDefinitionSearchService service = CreateDatasetSearchService(logger: logger, searchRepository: searchRepository);

            //Act
            IActionResult result = await service.SearchDatasetDefinitions(model);

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
        public async Task SearchDatasetDefinition_GivenValidModelWithMultipleOfSameFilter_ThenSearchIsPerformed()
        {
            //Arrange
            SearchModel model = new SearchModel
            {
                PageNumber = 1,
                Top = 50,
                IncludeFacets = true,
                Filters = new Dictionary<string, string[]>()
                {
                    { "providerIdentifier", new string []{ "test", "test2" } }
                },
                SearchTerm = "testTerm",
            };

            SearchResults<DatasetDefinitionIndex> searchResults = new SearchResults<DatasetDefinitionIndex>();

            ILogger logger = CreateLogger();

            ISearchRepository<DatasetDefinitionIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .Search(Arg.Any<string>(), Arg.Any<SearchParameters>())
                .Returns(searchResults);

            DatasetDefinitionSearchService service = CreateDatasetSearchService(logger: logger, searchRepository: searchRepository);

            //Act
            IActionResult result = await service.SearchDatasetDefinitions(model);

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
        public async Task SearchDatasetDefinition_GivenValidModelWithNullFilterWithMultipleOfSameFilter_ThenSearchIsPerformed()
        {
            //Arrange
            SearchModel model = new SearchModel
            {
                PageNumber = 1,
                Top = 50,
                IncludeFacets = true,
                Filters = new Dictionary<string, string[]>()
                {
                    { "providerIdentifier", new string []{ "test", "" } }
                },
                SearchTerm = "testTerm",
            };

            SearchResults<DatasetDefinitionIndex> searchResults = new SearchResults<DatasetDefinitionIndex>();

            ILogger logger = CreateLogger();

            ISearchRepository<DatasetDefinitionIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .Search(Arg.Any<string>(), Arg.Any<SearchParameters>())
                .Returns(searchResults);

            DatasetDefinitionSearchService service = CreateDatasetSearchService(logger: logger, searchRepository: searchRepository);

            //Act
            IActionResult result = await service.SearchDatasetDefinitions(model);

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
        public async Task SearchDatasetDefinition_GivenValidModelAndDoesntIncludeGettingFacets_CallsSearchOnce()
        {
            //Arrange
            SearchModel model = new SearchModel
            {
                PageNumber = 1,
                Top = 50
            };

            SearchResults<DatasetDefinitionIndex> searchResults = new SearchResults<DatasetDefinitionIndex>();

            ILogger logger = CreateLogger();

            ISearchRepository<DatasetDefinitionIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .Search(Arg.Any<string>(), Arg.Any<SearchParameters>())
                .Returns(searchResults);

            DatasetDefinitionSearchService service = CreateDatasetSearchService(logger: logger, searchRepository: searchRepository);

            //Act
            IActionResult result = await service.SearchDatasetDefinitions(model);

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
        public async Task SearchDatasetDefinition_GivenValidModelAndPageNumber2_CallsSearchWithCorrectSkipValue()
        {
            //Arrange
            const int skipValue = 50;

            SearchModel model = new SearchModel
            {
                PageNumber = 2,
                Top = 50
            };

            SearchResults<DatasetDefinitionIndex> searchResults = new SearchResults<DatasetDefinitionIndex>();

            ILogger logger = CreateLogger();

            ISearchRepository<DatasetDefinitionIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .Search(Arg.Any<string>(), Arg.Any<SearchParameters>())
                .Returns(searchResults);

            DatasetDefinitionSearchService service = CreateDatasetSearchService(logger: logger, searchRepository: searchRepository);

            //Act
            IActionResult result = await service.SearchDatasetDefinitions(model);

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
        public async Task SearchDatasetDefinition_GivenValidModelAndPageNumber10_CallsSearchWithCorrectSkipValue()
        {
            //Arrange
            const int skipValue = 450;

            SearchModel model = new SearchModel
            {
                PageNumber = 10,
                Top = 50
            };

            SearchResults<DatasetDefinitionIndex> searchResults = new SearchResults<DatasetDefinitionIndex>();

            ILogger logger = CreateLogger();

            ISearchRepository<DatasetDefinitionIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .Search(Arg.Any<string>(), Arg.Any<SearchParameters>())
                .Returns(searchResults);

            DatasetDefinitionSearchService service = CreateDatasetSearchService(logger: logger, searchRepository: searchRepository);

            //Act
            IActionResult result = await service.SearchDatasetDefinitions(model);

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
        public async Task SearchDatasetDefinition_GivenValidModel_CallsSearchWithCorrectSkipValue()
        {
            //Arrange
            SearchModel model = new SearchModel
            {
                PageNumber = 10,
                Top = 50,
                IncludeFacets = true
            };

            SearchResults<DatasetDefinitionIndex> searchResults = new SearchResults<DatasetDefinitionIndex>
            {
                Facets = new List<Facet>
                {
                    new Facet
                    {
                        Name = "specificationNames"
                    }
                }
            };

            ILogger logger = CreateLogger();

            ISearchRepository<DatasetDefinitionIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .Search(Arg.Any<string>(), Arg.Any<SearchParameters>())
                .Returns(searchResults);

            DatasetDefinitionSearchService service = CreateDatasetSearchService(logger: logger, searchRepository: searchRepository);

            //Act
            IActionResult result = await service.SearchDatasetDefinitions(model);

            //Assert
            result
                 .Should()
                 .BeOfType<OkObjectResult>();

            await
                searchRepository
                    .Received(4)
                    .Search(Arg.Any<string>(), Arg.Any<SearchParameters>());
        }

        static DatasetDefinitionSearchService CreateDatasetSearchService(
           ILogger logger = null, ISearchRepository<DatasetDefinitionIndex> searchRepository = null)
        {
            return new DatasetDefinitionSearchService(
                logger ?? CreateLogger(), searchRepository ?? CreateSearchRepository());
        }

        static ILogger CreateLogger()
        {
            return Substitute.For<ILogger>();
        }

        static ISearchRepository<DatasetDefinitionIndex> CreateSearchRepository()
        {
            return Substitute.For<ISearchRepository<DatasetDefinitionIndex>>();
        }
    }
}
