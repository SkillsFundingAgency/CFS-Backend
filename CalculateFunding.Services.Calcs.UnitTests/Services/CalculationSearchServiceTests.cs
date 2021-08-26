using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Models;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Versioning;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Repositories.Common.Search.Results;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Search.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Serilog;

namespace CalculateFunding.Services.Calcs.UnitTests.Services
{
    [TestClass]
    public class CalculationSearchServiceTests
    {
        //TODO; clean this all up and add actual mocking constraints instead of all this Any stuff that doesnt test anything

        [TestMethod]
        public async Task SearchCalculationsDelegatesToOverloadWithSearchModelParameter()
        {
            string searchTerm = NewRandomString();
            string specificationId = NewRandomString();
            int page  = NewRandomNumberBetween(1, 100);
            CalculationType calculationType = NewRandomEnum<CalculationType>();
            
            SearchResults<CalculationIndex> searchResults = new SearchResults<CalculationIndex>();

            ILogger logger = CreateLogger();

            ISearchRepository<CalculationIndex> searchRepository = CreateSearchRepository();

            string expectedSearchFilter = $"(specificationId eq '{specificationId}') and (calculationType eq '{calculationType}')";
            
            searchRepository
                .Search(searchTerm, 
                    Arg.Is<SearchParameters>(_ => 
                        _.SearchMode == SearchMode.All &&
                        _.SearchFields != null &&
                        _.SearchFields.SequenceEqual(new [] { "name" }) &&
                        _.Filter == expectedSearchFilter &&
                        _.IncludeTotalResultCount &&
                        _.QueryType == QueryType.Full &&
                        _.Top == 50 &&
                        _.Skip == (page - 1) * 50))
                .Returns(searchResults);

            CalculationSearchService service = CreateCalculationSearchService(logger: logger, serachRepository: searchRepository);

            OkObjectResult result = await service.SearchCalculations(specificationId, calculationType, null, searchTerm, page) as OkObjectResult;

            result?
                .Value
                .Should()
                .BeOfType<CalculationSearchResults>();

            await searchRepository
                .Received(1)
                .Search(searchTerm,
                    Arg.Is<SearchParameters>(_ =>
                        _.SearchMode == SearchMode.All &&
                        _.Filter == expectedSearchFilter &&
                        _.SearchFields != null &&
                        _.SearchFields.SequenceEqual(new [] { "name" }) &&
                        _.IncludeTotalResultCount &&
                        _.QueryType == QueryType.Full &&
                        _.Top == 50 &&
                        _.Skip == (page - 1) * 50));
        }
        
        [TestMethod]
        public async Task SearchCalculationsWithStatusSuppliedDelegatesToOverloadWithSearchModelParameter()
        {
            string searchTerm = NewRandomString();
            string specificationId = NewRandomString();
            int page  = NewRandomNumberBetween(1, 100);
            CalculationType calculationType = NewRandomEnum<CalculationType>();
            PublishStatus status = NewRandomEnum<PublishStatus>();
            
            SearchResults<CalculationIndex> searchResults = new SearchResults<CalculationIndex>();

            ILogger logger = CreateLogger();

            ISearchRepository<CalculationIndex> searchRepository = CreateSearchRepository();

            string expectedSearchFilter = $"(status eq '{status}') and (specificationId eq '{specificationId}') and (calculationType eq '{calculationType}')";
            
            searchRepository
                .Search(searchTerm, 
                    Arg.Is<SearchParameters>(_ => 
                        _.SearchMode == SearchMode.All &&
                        _.SearchFields != null &&
                        _.SearchFields.SequenceEqual(new [] { "name" }) &&
                        _.Filter == expectedSearchFilter &&
                        _.IncludeTotalResultCount &&
                        _.QueryType == QueryType.Full &&
                        _.Top == 50 &&
                        _.Skip == (page - 1) * 50))
                .Returns(searchResults);

            CalculationSearchService service = CreateCalculationSearchService(logger: logger, serachRepository: searchRepository);

            OkObjectResult result = await service.SearchCalculations(specificationId, calculationType, status, searchTerm, page) as OkObjectResult;

            result?
                .Value
                .Should()
                .BeOfType<CalculationSearchResults>();

            await searchRepository
                .Received(1)
                .Search(searchTerm,
                    Arg.Is<SearchParameters>(_ =>
                        _.SearchMode == SearchMode.All &&
                        _.Filter == expectedSearchFilter &&
                        _.SearchFields != null &&
                        _.SearchFields.SequenceEqual(new [] { "name" }) &&
                        _.IncludeTotalResultCount &&
                        _.QueryType == QueryType.Full &&
                        _.Top == 50 &&
                        _.Skip == (page - 1) * 50));
        }

        private string NewRandomString() => new RandomString();
        
        private int NewRandomNumberBetween(int start, int end) => new RandomNumberBetween(start, end);

        private TEnum NewRandomEnum<TEnum>()
            where TEnum : struct
        {
            return new RandomEnum<TEnum>();
        }

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

            ILogger logger = CreateLogger();

            ISearchRepository<CalculationIndex> searchRepository = CreateSearchRepository();

            searchRepository
                .When(s => s.Search(Arg.Any<string>(), Arg.Any<SearchParameters>()))
                    .Do(x => throw new FailedToQuerySearchException("Test Message", null));


            CalculationSearchService service = CreateCalculationSearchService(logger: logger, serachRepository: searchRepository);

            //Act
            IActionResult result = await service.SearchCalculations(model);

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
            ILogger logger = CreateLogger();

            ISearchRepository<CalculationIndex> searchRepository = CreateSearchRepository();

            CalculationSearchService service = CreateCalculationSearchService(logger: logger, serachRepository: searchRepository);

            //Act
            IActionResult result = await service.SearchCalculations(null);

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

            ILogger logger = CreateLogger();

            ISearchRepository<CalculationIndex> searchRepository = CreateSearchRepository();

            CalculationSearchService service = CreateCalculationSearchService(logger: logger, serachRepository: searchRepository);

            //Act
            IActionResult result = await service.SearchCalculations(model);

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


            ILogger logger = CreateLogger();

            ISearchRepository<CalculationIndex> searchRepository = CreateSearchRepository();

            CalculationSearchService service = CreateCalculationSearchService(logger: logger, serachRepository: searchRepository);

            //Act
            IActionResult result = await service.SearchCalculations(model);

            //Assert
            logger
                .Received(1)
                .Error("A null or invalid search model was provided for searching calculations");

            result
                 .Should()
                 .BeOfType<BadRequestObjectResult>();
        }

        [TestMethod]
        public async Task SearchCalculation_GivenValidModelAndIncludesGettingFacets_CallsSearchFourTimes()
        {
            //Arrange
            SearchModel model = new SearchModel
            {
                PageNumber = 1,
                Top = 50,
                IncludeFacets = true
            };

            SearchResults<CalculationIndex> searchResults = new SearchResults<CalculationIndex>();

            ILogger logger = CreateLogger();

            ISearchRepository<CalculationIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .Search(Arg.Any<string>(), Arg.Any<SearchParameters>())
                .Returns(searchResults);

            CalculationSearchService service = CreateCalculationSearchService(logger: logger, serachRepository: searchRepository);

            //Act
            IActionResult result = await service.SearchCalculations(model);

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

            SearchResults<CalculationIndex> searchResults = new SearchResults<CalculationIndex>();

            ILogger logger = CreateLogger();

            ISearchRepository<CalculationIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .Search(Arg.Any<string>(), Arg.Any<SearchParameters>())
                .Returns(searchResults);

            CalculationSearchService service = CreateCalculationSearchService(logger: logger, serachRepository: searchRepository);

            //Act
            IActionResult result = await service.SearchCalculations(model);

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
        public async Task SearchCalculation_GivenValidModelAndDoesntIncludeGettingFacets_CallsSearchOnce()
        {
            //Arrange
            SearchModel model = new SearchModel
            {
                PageNumber = 1,
                Top = 50
            };

            SearchResults<CalculationIndex> searchResults = new SearchResults<CalculationIndex>();

            ILogger logger = CreateLogger();

            ISearchRepository<CalculationIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .Search(Arg.Any<string>(), Arg.Any<SearchParameters>())
                .Returns(searchResults);

            CalculationSearchService service = CreateCalculationSearchService(logger: logger, serachRepository: searchRepository);

            //Act
            IActionResult result = await service.SearchCalculations(model);

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
        public async Task SearchCalculation_GivenValidModelAndIncludesSortExpression_CallsSearchOnce()
        {
            //Arrange
            IEnumerable<string> orderByExpression = new List<string> { "name asc" };

            SearchModel model = new SearchModel
            {
                PageNumber = 1,
                Top = 50,
                OrderBy = orderByExpression
            };

            SearchResults<CalculationIndex> searchResults = new SearchResults<CalculationIndex>();

            ILogger logger = CreateLogger();

            ISearchRepository<CalculationIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .Search(Arg.Any<string>(), Arg.Is<SearchParameters>(_ => _.OrderBy.Contains(orderByExpression.FirstOrDefault())))
                .Returns(searchResults);

            CalculationSearchService service = CreateCalculationSearchService(logger: logger, serachRepository: searchRepository);

            //Act
            IActionResult result = await service.SearchCalculations(model);

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

            SearchResults<CalculationIndex> searchResults = new SearchResults<CalculationIndex>();

            ILogger logger = CreateLogger();

            ISearchRepository<CalculationIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .Search(Arg.Any<string>(), Arg.Any<SearchParameters>())
                .Returns(searchResults);

            CalculationSearchService service = CreateCalculationSearchService(logger: logger, serachRepository: searchRepository);

            //Act
            IActionResult result = await service.SearchCalculations(model);

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

            SearchResults<CalculationIndex> searchResults = new SearchResults<CalculationIndex>();

            ILogger logger = CreateLogger();

            ISearchRepository<CalculationIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .Search(Arg.Any<string>(), Arg.Any<SearchParameters>())
                .Returns(searchResults);

            CalculationSearchService service = CreateCalculationSearchService(logger: logger, serachRepository: searchRepository);

            //Act
            IActionResult result = await service.SearchCalculations(model);

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
            IActionResult result = await service.SearchCalculations(model);

            //Assert
            result
                 .Should()
                 .BeOfType<OkObjectResult>();

            await
                searchRepository
                    .Received(4)
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
