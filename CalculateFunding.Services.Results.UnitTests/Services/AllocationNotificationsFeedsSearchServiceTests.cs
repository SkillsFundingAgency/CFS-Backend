using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Models.Results.Search;
using CalculateFunding.Models.Search;
using CalculateFunding.Repositories.Common.Search;
using FluentAssertions;
using Microsoft.Azure.Search.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace CalculateFunding.Services.Results.UnitTests.Services
{
    [TestClass]
    public class AllocationNotificationsFeedsSearchServiceTests
    {
        [TestMethod]
        public void GetFeeeds_GivenInvalidPageRef_ThrowsArgumentException()
        {
            //Arrange
            int pageRef = 0;

            AllocationNotificationsFeedsSearchService searchService = CreateSearchService();

            //Act
            Func<Task> test = async () => await searchService.GetFeeds(pageRef);

            //Assert
            test
                .Should()
                .ThrowExactly<ArgumentException>();
        }

        [TestMethod]
        public async Task GetFeeds_GivenInvalidTopPassed_DefaultsTopTo500()
        {
            //Arrange
            int pageRef = 1;

            int top = 0;

            ISearchRepository<AllocationNotificationFeedIndex> searchRepository = CreateSearchRepository();

            AllocationNotificationsFeedsSearchService searchService = CreateSearchService(searchRepository);

            //Act
            SearchFeed<AllocationNotificationFeedIndex> searchFeed = await searchService.GetFeeds(pageRef, top);

            //Assert
            await
                searchRepository
                .Received(1)
                .Search(Arg.Is(""), Arg.Is<SearchParameters>(m => m.Top == 500 && m.OrderBy.First() == "dateUpdated desc"));
        }

        [TestMethod]
        public async Task GetFeeds_GivenNoStatuses_SearchesWithNoFilter()
        {
            //Arrange
            int pageRef = 1;

            int top = 0;

            ISearchRepository<AllocationNotificationFeedIndex> searchRepository = CreateSearchRepository();

            AllocationNotificationsFeedsSearchService searchService = CreateSearchService(searchRepository);

            //Act
            SearchFeed<AllocationNotificationFeedIndex> searchFeed = await searchService.GetFeeds(pageRef, top);

            //Assert
            await
                searchRepository
                .Received(1)
                .Search(Arg.Is(""), Arg.Is<SearchParameters>(m => m.Filter == ""));
        }

        [TestMethod]
        public async Task GetFeeds_GivenPublishedStatus_SearchesWithFilter()
        {
            //Arrange
            int pageRef = 1;

            int top = 0;

            IEnumerable<string> statuses = new[] { "Published" };

            ISearchRepository<AllocationNotificationFeedIndex> searchRepository = CreateSearchRepository();

            AllocationNotificationsFeedsSearchService searchService = CreateSearchService(searchRepository);

            //Act
            SearchFeed<AllocationNotificationFeedIndex> searchFeed = await searchService.GetFeeds(pageRef, top, statuses);

            //Assert
            await
                searchRepository
                .Received(1)
                .Search(Arg.Is(""), Arg.Is<SearchParameters>(m => m.Filter == "allocationStatus eq 'Published'"));
        }

        [TestMethod]
        public async Task GetFeeds_GivenPublishedAndApprovedStatus_SearchesWithFilter()
        {
            //Arrange
            int pageRef = 1;

            int top = 0;

            IEnumerable<string> statuses = new[] { "Published", "Approved" };

            ISearchRepository<AllocationNotificationFeedIndex> searchRepository = CreateSearchRepository();

            AllocationNotificationsFeedsSearchService searchService = CreateSearchService(searchRepository);

            //Act
            SearchFeed<AllocationNotificationFeedIndex> searchFeed = await searchService.GetFeeds(pageRef, top, statuses);

            //Assert
            await
                searchRepository
                .Received(1)
                .Search(Arg.Is(""), Arg.Is<SearchParameters>(m => m.Filter == "allocationStatus eq 'Published' or allocationStatus eq 'Approved'"));
        }

        [TestMethod]
        public async Task GetFeeds_GivenPublishedAndApprovedStatusAndIncludesAll_SearchesWithNoFilter()
        {
            //Arrange
            int pageRef = 1;

            int top = 0;

            IEnumerable<string> statuses = new[] { "Published", "Approved", "All" };

            ISearchRepository<AllocationNotificationFeedIndex> searchRepository = CreateSearchRepository();

            AllocationNotificationsFeedsSearchService searchService = CreateSearchService(searchRepository);

            //Act
            SearchFeed<AllocationNotificationFeedIndex> searchFeed = await searchService.GetFeeds(pageRef, top, statuses);

            //Assert
            await
                searchRepository
                .Received(1)
                .Search(Arg.Is(""), Arg.Is<SearchParameters>(m => m.Filter == ""));
        }

        [TestMethod]
        public async Task GetFeeds_GivenResultsFound_ReturnsFeed()
        {
            //Arrange
            int pageRef = 1;

            int top = 0;

            SearchResults<AllocationNotificationFeedIndex> searchResults = new SearchResults<AllocationNotificationFeedIndex>
            {
                TotalCount = 2,
                Results = new List<CalculateFunding.Repositories.Common.Search.SearchResult<AllocationNotificationFeedIndex>>
                {
                    new CalculateFunding.Repositories.Common.Search.SearchResult<AllocationNotificationFeedIndex>
                    {
                        Result = new AllocationNotificationFeedIndex()
                    },
                    new CalculateFunding.Repositories.Common.Search.SearchResult<AllocationNotificationFeedIndex>
                    {
                        Result = new AllocationNotificationFeedIndex()
                    }
                }
            };

            ISearchRepository<AllocationNotificationFeedIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .Search(Arg.Is(""), Arg.Any<SearchParameters>())
                .Returns(searchResults);


            AllocationNotificationsFeedsSearchService searchService = CreateSearchService(searchRepository);

            //Act
            SearchFeed<AllocationNotificationFeedIndex> searchFeed = await searchService.GetFeeds(pageRef, top);

            //Assert
            searchFeed.Should().NotBeNull();
            searchFeed.Id.Should().NotBeEmpty();
            searchFeed.PageRef.Should().Be(1);
            searchFeed.Top.Should().Be(500);
            searchFeed.TotalCount.Should().Be(2);
            searchFeed.TotalPages.Should().Be(1);
            searchFeed.Self.Should().Be(1);
            searchFeed.Last.Should().Be(1);
            searchFeed.Next.Should().Be(1);
            searchFeed.Previous.Should().Be(1);
            searchFeed.Entries.Count().Should().Be(2);
        }

        [TestMethod]
        public async Task GetFeeds_GivenResultsFoundWithTotalCountAs20AndTopAsTwo_ReturnsFeed()
        {
            //Arrange
            int pageRef = 1;

            int top = 2;

            SearchResults<AllocationNotificationFeedIndex> searchResults = new SearchResults<AllocationNotificationFeedIndex>
            {
                TotalCount = 20,
                Results = new List<CalculateFunding.Repositories.Common.Search.SearchResult<AllocationNotificationFeedIndex>>
                {
                    new CalculateFunding.Repositories.Common.Search.SearchResult<AllocationNotificationFeedIndex>
                    {
                        Result = new AllocationNotificationFeedIndex()
                    },
                    new CalculateFunding.Repositories.Common.Search.SearchResult<AllocationNotificationFeedIndex>
                    {
                        Result = new AllocationNotificationFeedIndex()
                    }
                }
            };

            ISearchRepository<AllocationNotificationFeedIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .Search(Arg.Is(""), Arg.Any<SearchParameters>())
                .Returns(searchResults);


            AllocationNotificationsFeedsSearchService searchService = CreateSearchService(searchRepository);

            //Act
            SearchFeed<AllocationNotificationFeedIndex> searchFeed = await searchService.GetFeeds(pageRef, top);

            //Assert
            searchFeed.Should().NotBeNull();
            searchFeed.Id.Should().NotBeEmpty();
            searchFeed.PageRef.Should().Be(1);
            searchFeed.Top.Should().Be(2);
            searchFeed.TotalCount.Should().Be(20);
            searchFeed.TotalPages.Should().Be(10);
            searchFeed.Self.Should().Be(1);
            searchFeed.Last.Should().Be(10);
            searchFeed.Next.Should().Be(2);
            searchFeed.Previous.Should().Be(1);
            searchFeed.Entries.Count().Should().Be(2);
        }

        [TestMethod]
        public async Task GetFeeds_GivenResultsFoundWithTotalCountAs20AndTopAsTwoAndPageRefAs5_ReturnsFeed()
        {
            //Arrange
            int pageRef = 5;

            int top = 2;

            SearchResults<AllocationNotificationFeedIndex> searchResults = new SearchResults<AllocationNotificationFeedIndex>
            {
                TotalCount = 20,
                Results = new List<CalculateFunding.Repositories.Common.Search.SearchResult<AllocationNotificationFeedIndex>>
                {
                    new CalculateFunding.Repositories.Common.Search.SearchResult<AllocationNotificationFeedIndex>
                    {
                        Result = new AllocationNotificationFeedIndex()
                    },
                    new CalculateFunding.Repositories.Common.Search.SearchResult<AllocationNotificationFeedIndex>
                    {
                        Result = new AllocationNotificationFeedIndex()
                    }
                }
            };

            ISearchRepository<AllocationNotificationFeedIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .Search(Arg.Is(""), Arg.Any<SearchParameters>())
                .Returns(searchResults);


            AllocationNotificationsFeedsSearchService searchService = CreateSearchService(searchRepository);

            //Act
            SearchFeed<AllocationNotificationFeedIndex> searchFeed = await searchService.GetFeeds(pageRef, top);

            //Assert
            searchFeed.Should().NotBeNull();
            searchFeed.Id.Should().NotBeEmpty();
            searchFeed.PageRef.Should().Be(5);
            searchFeed.Top.Should().Be(2);
            searchFeed.TotalCount.Should().Be(20);
            searchFeed.TotalPages.Should().Be(10);
            searchFeed.Self.Should().Be(5);
            searchFeed.Last.Should().Be(10);
            searchFeed.Next.Should().Be(6);
            searchFeed.Previous.Should().Be(4);
            searchFeed.Entries.Count().Should().Be(2);
        }

        [TestMethod]
        public void GetFeedsV2_GivenInvalidPageRef_ThrowsArgumentException()
        {
            //Arrange
            int pageRef = 0;

            AllocationNotificationsFeedsSearchService searchService = CreateSearchService();

            //Act
            Func<Task> test = async () => await searchService.GetFeedsV2(pageRef, 500);

            //Assert
            test
                .Should()
                .ThrowExactly<ArgumentException>();
        }

        [TestMethod]
        public async Task GetFeedsV2_GivenInvalidTopPassed_DefaultsTopTo500()
        {
            //Arrange
            int pageRef = 1;

            int top = 0;

            ISearchRepository<AllocationNotificationFeedIndex> searchRepository = CreateSearchRepository();

            AllocationNotificationsFeedsSearchService searchService = CreateSearchService(searchRepository);

            //Act
            SearchFeedV2<AllocationNotificationFeedIndex> searchFeed = await searchService.GetFeedsV2(pageRef, top);

            //Assert
            await
                searchRepository
                    .Received(1)
                    .Search(Arg.Is(""), Arg.Is<SearchParameters>(m => m.Top == 500 && m.OrderBy.First() == "dateUpdated asc"));
        }

        [TestMethod]
        public async Task GetFeedsV2_GivenFilterValues_SearchesWithNoFilter()
        {
            //Arrange
            int pageRef = 1;

            int top = 0;

            ISearchRepository<AllocationNotificationFeedIndex> searchRepository = CreateSearchRepository();

            AllocationNotificationsFeedsSearchService searchService = CreateSearchService(searchRepository);

            //Act
            SearchFeedV2<AllocationNotificationFeedIndex> searchFeed = await searchService.GetFeedsV2(pageRef, top);

            //Assert
            await
                searchRepository
                .Received(1)
                .Search(Arg.Is(""), Arg.Is<SearchParameters>(m => m.Filter == ""));
        }

        [TestMethod]
        public async Task GetFeedsV2_GivenMultipleFilters_SearchesWithFilter()
        {
            //Arrange
            int pageRef = 1;

            int top = 0;

            IEnumerable<string> statuses = new[] { "Published", "Approved" };

            ISearchRepository<AllocationNotificationFeedIndex> searchRepository = CreateSearchRepository();

            AllocationNotificationsFeedsSearchService searchService = CreateSearchService(searchRepository);

            //Act
            SearchFeedV2<AllocationNotificationFeedIndex> searchFeed =
                await searchService.GetFeedsV2(pageRef, top, 2018, 2019, new[] { "12345678" }, new[] { "laCode" }, true, statuses, new[] { "PSG", "AAL" }, new[] { "PSG-001", "PSG-002" });

            //Assert
            await
                searchRepository
                    .Received(1)
                    .Search(Arg.Is(""),
                        Arg.Is<SearchParameters>(m =>
                            m.Filter ==
                            "(allocationStatus eq 'Published' or allocationStatus eq 'Approved') and (fundingStreamId eq 'PSG' or fundingStreamId eq 'AAL') and (allocationLineId eq 'PSG-001' or allocationLineId eq 'PSG-002') and (fundingPeriodStartYear eq 2018) and (fundingPeriodEndYear eq 2019) and (providerUkprn eq '12345678') and (laCode eq 'laCode') and (allocationLineContractRequired eq true)"));

        }

        [TestMethod]
        public async Task GetFeedsV2_GivenResultsFound_ReturnsFeed()
        {
            //Arrange
            int pageRef = 1;

            int top = 0;

            SearchResults<AllocationNotificationFeedIndex> searchResults = new SearchResults<AllocationNotificationFeedIndex>
            {
                TotalCount = 2,
                Results = new List<CalculateFunding.Repositories.Common.Search.SearchResult<AllocationNotificationFeedIndex>>
                {
                    new CalculateFunding.Repositories.Common.Search.SearchResult<AllocationNotificationFeedIndex>
                    {
                        Result = new AllocationNotificationFeedIndex()
                    },
                    new CalculateFunding.Repositories.Common.Search.SearchResult<AllocationNotificationFeedIndex>
                    {
                        Result = new AllocationNotificationFeedIndex()
                    }
                }
            };

            ISearchRepository<AllocationNotificationFeedIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .Search(Arg.Is(""), Arg.Any<SearchParameters>())
                .Returns(searchResults);


            AllocationNotificationsFeedsSearchService searchService = CreateSearchService(searchRepository);

            //Act
            SearchFeed<AllocationNotificationFeedIndex> searchFeed = await searchService.GetFeeds(pageRef, top);

            //Assert
            searchFeed.Should().NotBeNull();
            searchFeed.Id.Should().NotBeEmpty();
            searchFeed.PageRef.Should().Be(1);
            searchFeed.Top.Should().Be(500);
            searchFeed.TotalCount.Should().Be(2);
            searchFeed.TotalPages.Should().Be(1);
            searchFeed.Self.Should().Be(1);
            searchFeed.Last.Should().Be(1);
            searchFeed.Next.Should().Be(1);
            searchFeed.Previous.Should().Be(1);
            searchFeed.Entries.Count().Should().Be(2);
        }

        [TestMethod]
        public async Task GetFeeds_GivenNullPageRef_RetrievesLastPageOfFeedAndPerformsActualSearchAfter()
        {
            //Arrange
            int? pageRef = null;
            int top = 2;

            SearchResults<AllocationNotificationFeedIndex> searchResultsForCount = new SearchResults<AllocationNotificationFeedIndex>()
            {
                TotalCount = 20,
                Results = new List<CalculateFunding.Repositories.Common.Search.SearchResult<AllocationNotificationFeedIndex>>()
            };

            SearchResults<AllocationNotificationFeedIndex> searchResults = new SearchResults<AllocationNotificationFeedIndex>
            {
                TotalCount = 20,
                Results = new List<CalculateFunding.Repositories.Common.Search.SearchResult<AllocationNotificationFeedIndex>>
                {
                    new CalculateFunding.Repositories.Common.Search.SearchResult<AllocationNotificationFeedIndex>
                    {
                        Result = new AllocationNotificationFeedIndex()
                    },
                    new CalculateFunding.Repositories.Common.Search.SearchResult<AllocationNotificationFeedIndex>
                    {
                        Result = new AllocationNotificationFeedIndex()
                    }
                }
            };

            ISearchRepository<AllocationNotificationFeedIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .Search(Arg.Is(""), Arg.Is<SearchParameters>(sp => sp.Top == 0))
                .Returns(searchResultsForCount);

            searchRepository
                .Search(Arg.Is(""), Arg.Is<SearchParameters>(sp => sp.Top == 2))
                .Returns(searchResults);

            AllocationNotificationsFeedsSearchService searchService = CreateSearchService(searchRepository);

            //Act
            SearchFeedV2<AllocationNotificationFeedIndex> searchFeed = await searchService.GetFeedsV2(pageRef, top, statuses: new[] { "Published", "Approved" });

            //Assert
            searchFeed.Should().NotBeNull();
            searchFeed.Id.Should().NotBeEmpty();
            searchFeed.PageRef.Should().Be(10);
            searchFeed.Top.Should().Be(2);
            searchFeed.TotalCount.Should().Be(20);
            searchFeed.TotalPages.Should().Be(10);
            searchFeed.Current.Should().Be(null);
            searchFeed.Last.Should().Be(10);
            searchFeed.NextArchive.Should().BeNull();
            searchFeed.PreviousArchive.Should().Be(9);
            searchFeed.Entries.Count().Should().Be(2);
        }

        static AllocationNotificationsFeedsSearchService CreateSearchService(ISearchRepository<AllocationNotificationFeedIndex> searchRepository = null)
        {
            return new AllocationNotificationsFeedsSearchService(searchRepository ?? CreateSearchRepository(), ResultsResilienceTestHelper.GenerateTestPolicies());
        }

        static ISearchRepository<AllocationNotificationFeedIndex> CreateSearchRepository()
        {
            return Substitute.For<ISearchRepository<AllocationNotificationFeedIndex>>();
        }
    }
}
