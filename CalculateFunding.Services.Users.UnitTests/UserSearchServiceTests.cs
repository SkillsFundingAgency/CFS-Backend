using CalculateFunding.Models;
using CalculateFunding.Models.Users;
using CalculateFunding.Repositories.Common.Search;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Search.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Serilog;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Users.Services
{
    [TestClass]
    public class UserSearchServiceTests
    {
        [TestMethod]
        public async Task SearchUser_SearchRequestFails_ThenBadRequestReturned()
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

            ISearchRepository<UserIndex> searchRepository = CreateSearchRepository();

            searchRepository
                .When(s => s.Search(Arg.Any<string>(), Arg.Any<SearchParameters>()))
                    .Do(x => { throw new FailedToQuerySearchException("Test Message", null); });


            UserSearchService service = CreateUserSearchService(logger: logger, searchRepository: searchRepository);

            //Act
            IActionResult result = await service.SearchUsers(model);

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
        public async Task SearchUser_GivenNullSearchModel_LogsAndCreatesDefaultSearchModel()
        {
            //Arrange
            ILogger logger = CreateLogger();

            ISearchRepository<UserIndex> searchRepository = CreateSearchRepository();

            UserSearchService service = CreateUserSearchService(logger: logger, searchRepository: searchRepository);

            //Act
            IActionResult result = await service.SearchUsers(null);

            //Assert
            logger
                .Received(1)
                .Error("A null or invalid search model was provided for searching users");

            result
                 .Should()
                 .BeOfType<BadRequestObjectResult>();
        }

        [TestMethod]
        public async Task SearchUser_GivenPageNumberIsZero_LogsAndReturnsBadRequest()
        {
            //Arrange
            SearchModel model = new SearchModel();

            ILogger logger = CreateLogger();

            ISearchRepository<UserIndex> searchRepository = CreateSearchRepository();

            UserSearchService service = CreateUserSearchService(logger: logger, searchRepository: searchRepository);

            //Act
            IActionResult result = await service.SearchUsers(model);

            //Assert
            logger
                .Received(1)
                .Error("A null or invalid search model was provided for searching users");

            result
                 .Should()
                 .BeOfType<BadRequestObjectResult>();
        }

        [TestMethod]
        public async Task SearchUser_GivenPageTopIsZero_LogsAndReturnsBadRequest()
        {
            //Arrange
            SearchModel model = new SearchModel
            {
                PageNumber = 1,
                Top = 0
            };

            ILogger logger = CreateLogger();

            ISearchRepository<UserIndex> searchRepository = CreateSearchRepository();

            UserSearchService service = CreateUserSearchService(logger: logger, searchRepository: searchRepository);

            //Act
            IActionResult result = await service.SearchUsers(model);

            //Assert
            logger
                .Received(1)
                .Error("A null or invalid search model was provided for searching users");

            result
                 .Should()
                 .BeOfType<BadRequestObjectResult>();
        }

        [TestMethod]
        public async Task SearchUser_GivenValidModelAndIncludesGettingFacets_CallsSearchThreeTimes()
        {
            //Arrange
            SearchModel model = new SearchModel
            {
                PageNumber = 1,
                Top = 50,
                IncludeFacets = true
            };

            SearchResults<UserIndex> searchResults = new SearchResults<UserIndex>();

            ILogger logger = CreateLogger();

            ISearchRepository<UserIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .Search(Arg.Any<string>(), Arg.Any<SearchParameters>())
                .Returns(searchResults);

            UserSearchService service = CreateUserSearchService(logger: logger, searchRepository: searchRepository);

            //Act
            IActionResult result = await service.SearchUsers(model);

            //Assert
            result
                 .Should()
                 .BeOfType<OkObjectResult>();

            await
                searchRepository
                    .Received(3)
                    .Search(Arg.Any<string>(), Arg.Any<SearchParameters>());

        }

        [TestMethod]
        public async Task SearchUser_GivenValidModelWithNullFilters_ThenSearchIsStillPerformed()
        {
            //Arrange
            SearchModel model = new SearchModel
            {
                PageNumber = 1,
                Top = 50,
                IncludeFacets = true,
                Filters = null,
            };

            SearchResults<UserIndex> searchResults = new SearchResults<UserIndex>();

            ILogger logger = CreateLogger();

            ISearchRepository<UserIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .Search(Arg.Any<string>(), Arg.Any<SearchParameters>())
                .Returns(searchResults);

            UserSearchService service = CreateUserSearchService(logger: logger, searchRepository: searchRepository);

            //Act
            IActionResult result = await service.SearchUsers(model);

            //Assert
            result
                 .Should()
                 .BeOfType<OkObjectResult>();

            await
                searchRepository
                    .Received(3)
                    .Search(Arg.Any<string>(), Arg.Any<SearchParameters>());

        }

        [TestMethod]
        public async Task SearchUser_GivenValidModelWithOneFilter_ThenSearchIsPerformed()
        {
            //Arrange
            SearchModel model = new SearchModel
            {
                PageNumber = 1,
                Top = 50,
                IncludeFacets = true,
                Filters = new Dictionary<string, string[]>()
                {
                    { "userName", new string []{ "test" } }
                },
                SearchTerm = "testTerm",
            };

            SearchResults<UserIndex> searchResults = new SearchResults<UserIndex>();

            ILogger logger = CreateLogger();

            ISearchRepository<UserIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .Search(Arg.Any<string>(), Arg.Any<SearchParameters>())
                .Returns(searchResults);

            UserSearchService service = CreateUserSearchService(logger: logger, searchRepository: searchRepository);

            //Act
            IActionResult result = await service.SearchUsers(model);

            //Assert
            result
                 .Should()
                 .BeOfType<OkObjectResult>();

            await
                searchRepository
                .Received(2)
                    .Search(model.SearchTerm, Arg.Is<SearchParameters>(c =>
                        model.Filters.Keys.All(f => c.Filter.Contains(f))
                        && !string.IsNullOrWhiteSpace(c.Filter)
                    ));
        }

        [TestMethod]
        public async Task SearchUser_GivenValidModelWithMultipleOfSameFilter_ThenSearchIsPerformed()
        {
            //Arrange
            SearchModel model = new SearchModel
            {
                PageNumber = 1,
                Top = 50,
                IncludeFacets = true,
                Filters = new Dictionary<string, string[]>()
                {
                    { "userName", new string []{ "test", "test2" } }
                },
                SearchTerm = "testTerm",
            };

            SearchResults<UserIndex> searchResults = new SearchResults<UserIndex>();

            ILogger logger = CreateLogger();

            ISearchRepository<UserIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .Search(Arg.Any<string>(), Arg.Any<SearchParameters>())
                .Returns(searchResults);

            UserSearchService service = CreateUserSearchService(logger: logger, searchRepository: searchRepository);

            //Act
            IActionResult result = await service.SearchUsers(model);

            //Assert
            result
                 .Should()
                 .BeOfType<OkObjectResult>();

            await
                searchRepository
                .Received(2)
                    .Search(model.SearchTerm, Arg.Is<SearchParameters>(c =>
                        model.Filters.Keys.All(f => c.Filter.Contains(f))
                        && !string.IsNullOrWhiteSpace(c.Filter)
                    ));
        }

        [TestMethod]
        public async Task SearchUser_GivenValidModelWithNullFilterWithMultipleOfSameFilter_ThenSearchIsPerformed()
        {
            //Arrange
            SearchModel model = new SearchModel
            {
                PageNumber = 1,
                Top = 50,
                IncludeFacets = true,
                Filters = new Dictionary<string, string[]>()
                {
                    { "userName", new string []{ "test", "" } }
                },
                SearchTerm = "testTerm",
            };

            SearchResults<UserIndex> searchResults = new SearchResults<UserIndex>();

            ILogger logger = CreateLogger();

            ISearchRepository<UserIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .Search(Arg.Any<string>(), Arg.Any<SearchParameters>())
                .Returns(searchResults);

            UserSearchService service = CreateUserSearchService(logger: logger, searchRepository: searchRepository);

            //Act
            IActionResult result = await service.SearchUsers(model);

            //Assert
            result
                 .Should()
                 .BeOfType<OkObjectResult>();

            await
                searchRepository
                .Received(2)
                    .Search(model.SearchTerm, Arg.Is<SearchParameters>(c =>
                        model.Filters.Keys.All(f => c.Filter.Contains(f))
                        && !string.IsNullOrWhiteSpace(c.Filter)
                    ));
        }

        [TestMethod]
        public async Task SearchUser_GivenValidModelAndDoesntIncludeGettingFacets_CallsSearchOnce()
        {
            //Arrange
            SearchModel model = new SearchModel
            {
                PageNumber = 1,
                Top = 50
            };

            SearchResults<UserIndex> searchResults = new SearchResults<UserIndex>();

            ILogger logger = CreateLogger();

            ISearchRepository<UserIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .Search(Arg.Any<string>(), Arg.Any<SearchParameters>())
                .Returns(searchResults);

            UserSearchService service = CreateUserSearchService(logger: logger, searchRepository: searchRepository);

            //Act
            IActionResult result = await service.SearchUsers(model);

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
        public async Task SearchUser_GivenValidModelAndPageNumber2_CallsSearchWithCorrectSkipValue()
        {
            //Arrange
            const int skipValue = 50;

            SearchModel model = new SearchModel
            {
                PageNumber = 2,
                Top = 50
            };

            SearchResults<UserIndex> searchResults = new SearchResults<UserIndex>();

            ILogger logger = CreateLogger();

            ISearchRepository<UserIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .Search(Arg.Any<string>(), Arg.Any<SearchParameters>())
                .Returns(searchResults);

            UserSearchService service = CreateUserSearchService(logger: logger, searchRepository: searchRepository);

            //Act
            IActionResult result = await service.SearchUsers(model);

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
        public async Task SearchUser_GivenValidModelAndPageNumber10_CallsSearchWithCorrectSkipValue()
        {
            //Arrange
            const int skipValue = 450;

            SearchModel model = new SearchModel
            {
                PageNumber = 10,
                Top = 50
            };

            SearchResults<UserIndex> searchResults = new SearchResults<UserIndex>();

            ILogger logger = CreateLogger();

            ISearchRepository<UserIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .Search(Arg.Any<string>(), Arg.Any<SearchParameters>())
                .Returns(searchResults);

            UserSearchService service = CreateUserSearchService(logger: logger, searchRepository: searchRepository);

            //Act
            IActionResult result = await service.SearchUsers(model);

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
        public async Task SearchUser_GivenValidModel_CallsSearchWithCorrectSkipValue()
        {
            //Arrange
            SearchModel model = new SearchModel
            {
                PageNumber = 10,
                Top = 50,
                IncludeFacets = true
            };

            SearchResults<UserIndex> searchResults = new SearchResults<UserIndex>
            {
                Facets = new List<Facet>
                {
                    new Facet
                    {
                        Name = "userName"
                    }
                }
            };

            ILogger logger = CreateLogger();

            ISearchRepository<UserIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .Search(Arg.Any<string>(), Arg.Any<SearchParameters>())
                .Returns(searchResults);

            UserSearchService service = CreateUserSearchService(logger: logger, searchRepository: searchRepository);

            //Act
            IActionResult result = await service.SearchUsers(model);

            //Assert
            result
                 .Should()
                 .BeOfType<OkObjectResult>();

            await
                searchRepository
                    .Received(3)
                    .Search(Arg.Any<string>(), Arg.Any<SearchParameters>());
        }

	    static UserSearchService CreateUserSearchService(
		    ILogger logger = null,
		    ISearchRepository<UserIndex> searchRepository = null)
	    {
		    return new UserSearchService(
			    logger ?? CreateLogger(),
                searchRepository ?? CreateSearchRepository());
	    }

	    static ILogger CreateLogger()
        {
            return Substitute.For<ILogger>();
        }

        static ISearchRepository<UserIndex> CreateSearchRepository()
        {
            return Substitute.For<ISearchRepository<UserIndex>>();
        }
    }
}
