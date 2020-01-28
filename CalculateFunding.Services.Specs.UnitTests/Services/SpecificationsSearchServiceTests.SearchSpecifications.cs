using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CalculateFunding.Models;
using CalculateFunding.Models.Specs;
using CalculateFunding.Repositories.Common.Search;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Search.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using NSubstitute;
using Serilog;

namespace CalculateFunding.Services.Specs.UnitTests.Services
{
    public partial class SpecificationsSearchServiceTests
    {
        [TestMethod]
        public async Task SearchSpecifications_GivenNullSearchModel_ReturnsBadRequest()
        {
            //Arrange
            ILogger logger = CreateLogger();

            SpecificationsSearchService service = CreateSearchService(logger: logger);

            //Act
            IActionResult result = await service.SearchSpecifications(null);

            //Assert
            logger
                .Received(1)
                .Error("A null or invalid search model was provided for searching specifications");

            result
                .Should()
                .BeOfType<BadRequestObjectResult>();
        }

        [TestMethod]
        public async Task SearchSpecifications_GivenInvalidPageNumber_ReturnsBadRequest()
        {
            //Arrange
            SearchModel model = new SearchModel
            {
                PageNumber = 0,
                Top = 1
            };
            
            ILogger logger = CreateLogger();

            SpecificationsSearchService service = CreateSearchService(logger: logger);

            //Act
            IActionResult result = await service.SearchSpecifications(model);

            //Assert
            logger
                .Received(1)
                .Error("A null or invalid search model was provided for searching specifications");

            result
                .Should()
                .BeOfType<BadRequestObjectResult>();
        }

        [TestMethod]
        public async Task SearchSpecifications_GivenInvalidTop_ReturnsBadRequest()
        {
            //Arrange
            SearchModel model = new SearchModel
            {
                PageNumber = 1,
                Top = 0
            };
            
            ILogger logger = CreateLogger();

            SpecificationsSearchService service = CreateSearchService(logger: logger);

            //Act
            IActionResult result = await service.SearchSpecifications(model);

            //Assert
            logger
                .Received(1)
                .Error("A null or invalid search model was provided for searching specifications");

            result
                .Should()
                .BeOfType<BadRequestObjectResult>();
        }

        [TestMethod]
        public async Task SearchSpecifications_GivenValidModelAndIncludesGettingFacets_CallsSearchFourTimes()
        {
            //Arrange
            SearchModel model = new SearchModel
            {
                PageNumber = 1,
                Top = 50,
                IncludeFacets = true
            };

            SearchResults<SpecificationIndex> searchResults = new SearchResults<SpecificationIndex>();

            ILogger logger = CreateLogger();

            ISearchRepository<SpecificationIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .Search(Arg.Any<string>(), Arg.Any<SearchParameters>())
                .Returns(searchResults);

            SpecificationsSearchService service = CreateSearchService(logger: logger, searchRepository: searchRepository);

            //Act
            IActionResult result = await service.SearchSpecifications(model);

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
        public async Task SearchSpecifications_GivenValidModelWithNullFilters_ThenSearchIsStillPerformed()
        {
            //Arrange
            SearchModel model = new SearchModel
            {
                PageNumber = 1,
                Top = 50,
                IncludeFacets = true,
                Filters = null,
            };

            SearchResults<SpecificationIndex> searchResults = new SearchResults<SpecificationIndex>();

            ILogger logger = CreateLogger();

            ISearchRepository<SpecificationIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .Search(Arg.Any<string>(), Arg.Any<SearchParameters>())
                .Returns(searchResults);

            SpecificationsSearchService service = CreateSearchService(logger: logger, searchRepository: searchRepository);

            //Act
            IActionResult result = await service.SearchSpecifications(model);

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
        public async Task SearchSpecifications_GivenValidModelWithOneFilter_ThenSearchIsPerformed()
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

            SearchResults<SpecificationIndex> searchResults = new SearchResults<SpecificationIndex>();

            ILogger logger = CreateLogger();

            ISearchRepository<SpecificationIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .Search(Arg.Any<string>(), Arg.Any<SearchParameters>())
                .Returns(searchResults);

            SpecificationsSearchService service = CreateSearchService(logger: logger, searchRepository: searchRepository);

            //Act
            IActionResult result = await service.SearchSpecifications(model);

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
        public async Task SearchSpecifications_GivenValidModelWithOneFilterWhichIsMultiValueFacet_ThenSearchIsPerformed()
        {
            //Arrange
            SearchModel model = new SearchModel
            {
                PageNumber = 1,
                Top = 50,
                IncludeFacets = true,
                Filters = new Dictionary<string, string[]>()
                {
                    { "fundingStreamNames", new string []{ "test" } }
                },
                SearchTerm = "testTerm",
            };

            SearchResults<SpecificationIndex> searchResults = new SearchResults<SpecificationIndex>();

            ILogger logger = CreateLogger();

            ISearchRepository<SpecificationIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .Search(Arg.Any<string>(), Arg.Any<SearchParameters>())
                .Returns(searchResults);

            SpecificationsSearchService service = CreateSearchService(logger: logger, searchRepository: searchRepository);

            //Act
            IActionResult result = await service.SearchSpecifications(model);

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
        public async Task SearchSpecifications_GivenValidModelWithMultipleFilterValuesWhichIsMultiValueFacet_ThenSearchIsPerformed()
        {
            //Arrange
            SearchModel model = new SearchModel
            {
                PageNumber = 1,
                Top = 50,
                IncludeFacets = true,
                Filters = new Dictionary<string, string[]>()
                {
                    { "fundingStreamNames", new string []{ "test", "test2" } }
                },
                SearchTerm = "testTerm",
            };

            SearchResults<SpecificationIndex> searchResults = new SearchResults<SpecificationIndex>();

            ILogger logger = CreateLogger();

            ISearchRepository<SpecificationIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .Search(Arg.Any<string>(), Arg.Any<SearchParameters>())
                .Returns(searchResults);

            SpecificationsSearchService service = CreateSearchService(logger: logger, searchRepository: searchRepository);

            //Act
            IActionResult result = await service.SearchSpecifications(model);

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
        public async Task SearchSpecifications_GivenValidModelWithMultipleOfSameFilter_ThenSearchIsPerformed()
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

            SearchResults<SpecificationIndex> searchResults = new SearchResults<SpecificationIndex>();

            ILogger logger = CreateLogger();

            ISearchRepository<SpecificationIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .Search(Arg.Any<string>(), Arg.Any<SearchParameters>())
                .Returns(searchResults);

            SpecificationsSearchService service = CreateSearchService(logger: logger, searchRepository: searchRepository);

            //Act
            IActionResult result = await service.SearchSpecifications(model);

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
        public async Task SearchSpecifications_GivenValidModelWithNullFilterWithMultipleOfSameFilter_ThenSearchIsPerformed()
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

            SearchResults<SpecificationIndex> searchResults = new SearchResults<SpecificationIndex>();

            ILogger logger = CreateLogger();

            ISearchRepository<SpecificationIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .Search(Arg.Any<string>(), Arg.Any<SearchParameters>())
                .Returns(searchResults);

            SpecificationsSearchService service = CreateSearchService(logger: logger, searchRepository: searchRepository);

            //Act
            IActionResult result = await service.SearchSpecifications(model);

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
        public async Task SearchSpecifications_GivenValidModelAndDoesntIncludeGettingFacets_CallsSearchOnce()
        {
            //Arrange
            SearchModel model = new SearchModel
            {
                PageNumber = 1,
                Top = 50
            };

            SearchResults<SpecificationIndex> searchResults = new SearchResults<SpecificationIndex>();

            ILogger logger = CreateLogger();

            ISearchRepository<SpecificationIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .Search(Arg.Any<string>(), Arg.Any<SearchParameters>())
                .Returns(searchResults);

            SpecificationsSearchService service = CreateSearchService(logger: logger, searchRepository: searchRepository);

            //Act
            IActionResult result = await service.SearchSpecifications(model);

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
        public async Task SearchSpecifications_GivenValidModelAndPageNumber2_CallsSearchWithCorrectSkipValue()
        {
            //Arrange
            const int skipValue = 50;

            SearchModel model = new SearchModel
            {
                PageNumber = 2,
                Top = 50
            };

            SearchResults<SpecificationIndex> searchResults = new SearchResults<SpecificationIndex>();

            ILogger logger = CreateLogger();

            ISearchRepository<SpecificationIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .Search(Arg.Any<string>(), Arg.Any<SearchParameters>())
                .Returns(searchResults);

            SpecificationsSearchService service = CreateSearchService(logger: logger, searchRepository: searchRepository);

            //Act
            IActionResult result = await service.SearchSpecifications(model);

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
        public async Task SearchSpecifications_GivenValidModelAndPageNumber10_CallsSearchWithCorrectSkipValue()
        {
            //Arrange
            const int skipValue = 450;

            SearchModel model = new SearchModel
            {
                PageNumber = 10,
                Top = 50
            };

            SearchResults<SpecificationIndex> searchResults = new SearchResults<SpecificationIndex>();

            ILogger logger = CreateLogger();

            ISearchRepository<SpecificationIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .Search(Arg.Any<string>(), Arg.Any<SearchParameters>())
                .Returns(searchResults);

            SpecificationsSearchService service = CreateSearchService(logger: logger, searchRepository: searchRepository);

            //Act
            IActionResult result = await service.SearchSpecifications(model);

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
        public async Task SearchSpecifications_GivenValidModel_CallsSearchWithCorrectSkipValue()
        {
            //Arrange
            SearchModel model = new SearchModel
            {
                PageNumber = 10,
                Top = 50,
                IncludeFacets = true
            };

            SearchResults<SpecificationIndex> searchResults = new SearchResults<SpecificationIndex>
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

            ISearchRepository<SpecificationIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .Search(Arg.Any<string>(), Arg.Any<SearchParameters>())
                .Returns(searchResults);

            SpecificationsSearchService service = CreateSearchService(logger: logger, searchRepository: searchRepository);

            //Act
            IActionResult result = await service.SearchSpecifications(model);

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
