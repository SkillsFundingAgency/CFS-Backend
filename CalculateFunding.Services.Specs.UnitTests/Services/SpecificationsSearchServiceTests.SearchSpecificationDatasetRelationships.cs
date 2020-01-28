using System;
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
        public async Task SearchSpecificationDatasetRelationships_GivenNullSearchModel_ReturnsBadRequest()
        {
            //Arrange
            ILogger logger = CreateLogger();

            SpecificationsSearchService service = CreateSearchService(logger: logger);

            //Act
            IActionResult result = await service.SearchSpecificationDatasetRelationships(null);

            //Assert
            logger
                .Received(1)
                .Error("A null or invalid search model was provided for searching specifications");

            result
                .Should()
                .BeOfType<BadRequestObjectResult>();
        }

        [TestMethod]
        public async Task SearchSpecificationDatasetRelationships_GivenInvalidPageNumber_ReturnsBadRequest()
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
            IActionResult result = await service.SearchSpecificationDatasetRelationships(model);

            //Assert
            logger
                .Received(1)
                .Error("A null or invalid search model was provided for searching specifications");

            result
                .Should()
                .BeOfType<BadRequestObjectResult>();
        }

        [TestMethod]
        public async Task SearchSpecificationDatasetRelationships_GivenInvalidTop_ReturnsBadRequest()
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
            IActionResult result = await service.SearchSpecificationDatasetRelationships(model);

            //Assert
            logger
                .Received(1)
                .Error("A null or invalid search model was provided for searching specifications");

            result
                .Should()
                .BeOfType<BadRequestObjectResult>();
        }

        [TestMethod]
        async public Task SearchSpecificationDatasetRelationships_GivenSearchThrowsException_ReturnsStatusCode500()
        {
            //Arrange
            SearchModel model = CreateSearchModel();
            
            ISearchRepository<SpecificationIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .When(x => x.Search(Arg.Any<string>(), Arg.Any<SearchParameters>()))
                .Do(x => { throw new FailedToQuerySearchException("main", new Exception("inner")); });

            ILogger logger = CreateLogger();

            SpecificationsSearchService service = CreateSearchService(searchRepository, logger);

            //Act
            IActionResult result = await service.SearchSpecificationDatasetRelationships(model);

            //Assert
            result
                .Should()
                .BeOfType<StatusCodeResult>();

            StatusCodeResult statusCodeResult = result as StatusCodeResult;

            statusCodeResult
                .StatusCode
                .Should()
                .Be(500);
        }

        [TestMethod]
        async public Task SearchSpecificationDatasetRelationships_GivenSearchReturnsResults_ReturnsOKResult()
        {
            //Arrange
            SearchModel model = CreateSearchModel();
            
            SearchResults<SpecificationIndex> searchResults = new SearchResults<SpecificationIndex>
            {
                TotalCount = 1
            };

            ISearchRepository<SpecificationIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .Search(Arg.Is("SearchTermTest"), Arg.Any<SearchParameters>())
                .Returns(searchResults);

            ILogger logger = CreateLogger();

            SpecificationsSearchService service = CreateSearchService(searchRepository, logger);

            //Act
            IActionResult result = await service.SearchSpecificationDatasetRelationships(model);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();
        }

        [TestMethod]
        async public Task SearchSpecificationDatasetRelationships_GivenSearchReturnsResultsAndDataDefinitionsIsNull_ReturnsOKResult()
        {
            //Arrange
            SearchModel model = CreateSearchModel();
            
            SearchResults<SpecificationIndex> searchResults = new SearchResults<SpecificationIndex>
            {
                TotalCount = 1,
                Results = new List<Repositories.Common.Search.SearchResult<SpecificationIndex>>
                {
                    new Repositories.Common.Search.SearchResult<SpecificationIndex>
                    {
                        Result = new SpecificationIndex
                        {
                            DataDefinitionRelationshipIds = null
                        }
                    }
                }
            };

            ISearchRepository<SpecificationIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .Search(Arg.Is("SearchTermTest"), Arg.Any<SearchParameters>())
                .Returns(searchResults);

            ILogger logger = CreateLogger();

            SpecificationsSearchService service = CreateSearchService(searchRepository, logger);

            //Act
            IActionResult result = await service.SearchSpecificationDatasetRelationships(model);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();

            OkObjectResult okObjectResult = result as OkObjectResult;

            SpecificationDatasetRelationshipsSearchResults specificationSearchResults = okObjectResult.Value as SpecificationDatasetRelationshipsSearchResults;

            specificationSearchResults
                .Results
                .Count()
                .Should()
                .Be(1);

            specificationSearchResults
                .TotalCount
                .Should()
                .Be(1);

            specificationSearchResults
               .Results
               .First()
               .DefinitionRelationshipCount
               .Should()
               .Be(0);
        }

        [TestMethod]
        async public Task SearchSpecificationDatasetRelationships_GivenSearchReturnsResultsAndDataDefinitionsHasItems_ReturnsOKResult()
        {
            //Arrange
            SearchModel model = CreateSearchModel();
            
            SearchResults<SpecificationIndex> searchResults = new SearchResults<SpecificationIndex>
            {
                TotalCount = 1,
                Results = new List<Repositories.Common.Search.SearchResult<SpecificationIndex>>
                {
                    new Repositories.Common.Search.SearchResult<SpecificationIndex>
                    {
                        Result = new SpecificationIndex
                        {
                            DataDefinitionRelationshipIds = new[]{"def-1", "def-2"}
                        }
                    }
                }
            };

            ISearchRepository<SpecificationIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .Search(Arg.Is("SearchTermTest"), Arg.Any<SearchParameters>())
                .Returns(searchResults);

            ILogger logger = CreateLogger();

            SpecificationsSearchService service = CreateSearchService(searchRepository, logger);

            //Act
            IActionResult result = await service.SearchSpecificationDatasetRelationships(model);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();

            OkObjectResult okObjectResult = result as OkObjectResult;

            SpecificationDatasetRelationshipsSearchResults specificationSearchResults = okObjectResult.Value as SpecificationDatasetRelationshipsSearchResults;

            specificationSearchResults
                .Results
                .Count()
                .Should()
                .Be(1);

            specificationSearchResults
                .TotalCount
                .Should()
                .Be(1);

            specificationSearchResults
               .Results
               .First()
               .DefinitionRelationshipCount
               .Should()
               .Be(2);
        }
    }
}
