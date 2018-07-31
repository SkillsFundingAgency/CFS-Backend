using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;
using NSubstitute;
using CalculateFunding.Services.Results.Interfaces;
using System.Threading.Tasks;
using CalculateFunding.Api.External.V1.Services;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using Microsoft.AspNetCore.Http;
using FluentAssertions;
using CalculateFunding.Models.Results;
using CalculateFunding.Models.Search;
using Microsoft.Extensions.Primitives;
using CalculateFunding.Models.External.AtomItems;
using Newtonsoft.Json;
using CalculateFunding.Api.External.V1.Models;

namespace CalculateFunding.Api.External.UnitTests.Services
{
    [TestClass]
    public class AllocationNotificationFeedsServiceTests
    {
        [TestMethod]
        public async Task GetNotifications_GivenNoPageRefSupplied_DefaultsToPageOne()
        {
            //Arrange
            IAllocationNotificationsFeedsSearchService feedsSearchService = CreateSearchService();

            AllocationNotificationFeedsService service = CreateService(feedsSearchService);

            HttpRequest request = Substitute.For<HttpRequest>();

            //Act
            IActionResult result = await service.GetNotifications(null, "", request);

            //Assert
            await
            feedsSearchService
                .Received(1)
                .GetFeeds(Arg.Is(1), Arg.Is(500), Arg.Is<string[]>(m => m.First() == "Published"));
        }

        [TestMethod]
        public async Task GetNotifications_GivenPageRefOfThreeSupplied_RequestsPageThree()
        {
            //Arrange
            IAllocationNotificationsFeedsSearchService feedsSearchService = CreateSearchService();

            AllocationNotificationFeedsService service = CreateService(feedsSearchService);

            HttpRequest request = Substitute.For<HttpRequest>();

            //Act
            IActionResult result = await service.GetNotifications(3, "", request);

            //Assert
            await
            feedsSearchService
                .Received(1)
                .GetFeeds(Arg.Is(3), Arg.Is(500), Arg.Is<string[]>(m => m.First() == "Published"));
        }

        [TestMethod]
        public async Task GetNotifications_GivenMultipleStatuses_RequestsWithMultipleStatuses()
        {
            //Arrange
            IAllocationNotificationsFeedsSearchService feedsSearchService = CreateSearchService();

            AllocationNotificationFeedsService service = CreateService(feedsSearchService);

            HttpRequest request = Substitute.For<HttpRequest>();

            //Act
            IActionResult result = await service.GetNotifications(3, "Published,Approved", request);

            //Assert
            await
            feedsSearchService
                .Received(1)
                .GetFeeds(Arg.Is(3), Arg.Is(500), Arg.Is<string[]>(m => m.First() == "Published" && m.Last() == "Approved"));
        }

        [TestMethod]
        public async Task GetNotifications_GivenInvallidPageRef_ReturnsBadRequest()
        {
            //Arrange
            AllocationNotificationFeedsService service = CreateService();

            HttpRequest request = Substitute.For<HttpRequest>();

            //Act
            IActionResult result = await service.GetNotifications(-1, "", request);

            //Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>()
                .Which
                .Value
                .Should()
                .Be("Page ref should be at least 1");
        }

        [TestMethod]
        public async Task GetNotifications_GivenSearchFeedRetunsNoResults_ReturnsNotFoundResult()
        {
            //Arrange
            SearchFeed<AllocationNotificationFeedIndex> feeds = new SearchFeed<AllocationNotificationFeedIndex>();

            IAllocationNotificationsFeedsSearchService feedsSearchService = CreateSearchService();
            feedsSearchService
                .GetFeeds(Arg.Is(3), Arg.Is(500), Arg.Any<IEnumerable<string>>())
                .Returns(feeds);

            AllocationNotificationFeedsService service = CreateService(feedsSearchService);

            HttpRequest request = Substitute.For<HttpRequest>();

            //Act
            IActionResult result = await service.GetNotifications(3, "Published,Approved", request);

            //Assert
            result
                .Should()
                .BeOfType<NotFoundResult>();
        }

        [TestMethod]
        public async Task GetNotifications_GivenSearchFeedRetunsNoResults_ReturnsAtomFeed()
        {
            //Arrange
            SearchFeed<AllocationNotificationFeedIndex> feeds = new SearchFeed<AllocationNotificationFeedIndex>
            {
                PageRef = 3,
                Top = 500,
                TotalCount = 3,
                Entries = CreateFeedIndexes()
            };

            IAllocationNotificationsFeedsSearchService feedsSearchService = CreateSearchService();
            feedsSearchService
                .GetFeeds(Arg.Is(3), Arg.Is(500), Arg.Any<IEnumerable<string>>())
                .Returns(feeds);

            AllocationNotificationFeedsService service = CreateService(feedsSearchService);

            IHeaderDictionary headerDictionary = new HeaderDictionary();
            headerDictionary.Add("Accept", new StringValues("application/json"));

            HttpRequest request = Substitute.For<HttpRequest>();
            request.Scheme.Returns("https");
            request.Path.Returns(new PathString("/api/v1/test/3"));
            request.Host.Returns(new HostString("wherever.naf:12345"));
            request.QueryString.Returns(new QueryString("?allocationStatuses=Published,Approved"));
            request.Headers.Returns(headerDictionary);

            //Act
            IActionResult result = await service.GetNotifications(3, "Published,Approved", request);

            //Assert
            result
                .Should()
                .BeOfType<ContentResult>();

            ContentResult contentResult = result as ContentResult;
            AtomFeed<AllocationModel> atomFeed = JsonConvert.DeserializeObject<AtomFeed<AllocationModel>>(contentResult.Content);

            atomFeed
                .Should()
                .NotBeNull();

            atomFeed.Id.Should().NotBeEmpty();
            atomFeed.Title.Should().Be("Calculate Funding Service Allocation Feed");
            atomFeed.Author.Name.Should().Be("Calculate Funding Service");
            atomFeed.Link.First(m => m.Rel == "self").Href.Should().Be("https://wherever.naf:12345/api/v1/test/3?allocationStatuses=Published,Approved");
            atomFeed.Link.First(m => m.Rel == "first").Href.Should().Be("https://wherever.naf:12345/api/v1/test/1?allocationStatuses=Published,Approved");
            atomFeed.Link.First(m => m.Rel == "last").Href.Should().Be("https://wherever.naf:12345/api/v1/test/1?allocationStatuses=Published,Approved");
            atomFeed.Link.First(m => m.Rel == "previous").Href.Should().Be("https://wherever.naf:12345/api/v1/test/2?allocationStatuses=Published,Approved");
            atomFeed.Link.First(m => m.Rel == "next").Href.Should().Be("https://wherever.naf:12345/api/v1/test/3?allocationStatuses=Published,Approved");
            atomFeed.AtomEntry.Count.Should().Be(3);
            atomFeed.AtomEntry.ElementAt(0).Id.Should().Be("id-1");
            atomFeed.AtomEntry.ElementAt(0).Title.Should().Be("test title 1");
            atomFeed.AtomEntry.ElementAt(0).Summary.Should().Be("test summary 1");
            atomFeed.AtomEntry.ElementAt(0).Content.Allocation.FundingStream.FundingStreamCode.Should().Be("fs-1");
            atomFeed.AtomEntry.ElementAt(0).Content.Allocation.FundingStream.FundingStreamName.Should().Be("fs 1");
            atomFeed.AtomEntry.ElementAt(0).Content.Allocation.Period.PeriodType.Should().Be("fp type 1");
            atomFeed.AtomEntry.ElementAt(0).Content.Allocation.Period.PeriodId.Should().Be("fp-1");
            atomFeed.AtomEntry.ElementAt(0).Content.Allocation.Provider.Ukprn.Should().Be("1111");
            atomFeed.AtomEntry.ElementAt(0).Content.Allocation.Provider.Upin.Should().Be("0001");
            atomFeed.AtomEntry.ElementAt(0).Content.Allocation.AllocationLine.AllocationLineCode.Should().Be("al-1");
            atomFeed.AtomEntry.ElementAt(0).Content.Allocation.AllocationLine.AllocationLineName.Should().Be("al 1");
            atomFeed.AtomEntry.ElementAt(0).Content.Allocation.AllocationVersionNumber.Should().Be(1);
            atomFeed.AtomEntry.ElementAt(0).Content.Allocation.AllocationStatus.Should().Be("Published");
            atomFeed.AtomEntry.ElementAt(0).Content.Allocation.AllocationAmount.Should().Be(10);
            atomFeed.AtomEntry.ElementAt(0).Content.Allocation.ProfilePeriods.Length.Should().Be(0);
            atomFeed.AtomEntry.ElementAt(1).Id.Should().Be("id-2");
            atomFeed.AtomEntry.ElementAt(1).Title.Should().Be("test title 2");
            atomFeed.AtomEntry.ElementAt(1).Summary.Should().Be("test summary 2");
            atomFeed.AtomEntry.ElementAt(1).Content.Allocation.FundingStream.FundingStreamCode.Should().Be("fs-2");
            atomFeed.AtomEntry.ElementAt(1).Content.Allocation.FundingStream.FundingStreamName.Should().Be("fs 2");
            atomFeed.AtomEntry.ElementAt(1).Content.Allocation.Period.PeriodType.Should().Be("fp type 2");
            atomFeed.AtomEntry.ElementAt(1).Content.Allocation.Period.PeriodId.Should().Be("fp-2");
            atomFeed.AtomEntry.ElementAt(1).Content.Allocation.Provider.Ukprn.Should().Be("2222");
            atomFeed.AtomEntry.ElementAt(1).Content.Allocation.Provider.Upin.Should().Be("0002");
            atomFeed.AtomEntry.ElementAt(1).Content.Allocation.AllocationLine.AllocationLineCode.Should().Be("al-2");
            atomFeed.AtomEntry.ElementAt(1).Content.Allocation.AllocationLine.AllocationLineName.Should().Be("al 2");
            atomFeed.AtomEntry.ElementAt(1).Content.Allocation.AllocationVersionNumber.Should().Be(1);
            atomFeed.AtomEntry.ElementAt(1).Content.Allocation.AllocationStatus.Should().Be("Published");
            atomFeed.AtomEntry.ElementAt(1).Content.Allocation.AllocationAmount.Should().Be(100);
            atomFeed.AtomEntry.ElementAt(1).Content.Allocation.ProfilePeriods.Length.Should().Be(2);
            atomFeed.AtomEntry.ElementAt(2).Id.Should().Be("id-3");
            atomFeed.AtomEntry.ElementAt(2).Title.Should().Be("test title 3");
            atomFeed.AtomEntry.ElementAt(2).Summary.Should().Be("test summary 3");
            atomFeed.AtomEntry.ElementAt(2).Content.Allocation.FundingStream.FundingStreamCode.Should().Be("fs-3");
            atomFeed.AtomEntry.ElementAt(2).Content.Allocation.FundingStream.FundingStreamName.Should().Be("fs 3");
            atomFeed.AtomEntry.ElementAt(2).Content.Allocation.Period.PeriodType.Should().Be("fp type 3");
            atomFeed.AtomEntry.ElementAt(2).Content.Allocation.Period.PeriodId.Should().Be("fp-3");
            atomFeed.AtomEntry.ElementAt(2).Content.Allocation.Provider.Ukprn.Should().Be("3333");
            atomFeed.AtomEntry.ElementAt(2).Content.Allocation.Provider.Upin.Should().Be("0003");
            atomFeed.AtomEntry.ElementAt(2).Content.Allocation.AllocationLine.AllocationLineCode.Should().Be("al-3");
            atomFeed.AtomEntry.ElementAt(2).Content.Allocation.AllocationLine.AllocationLineName.Should().Be("al 3");
            atomFeed.AtomEntry.ElementAt(2).Content.Allocation.AllocationVersionNumber.Should().Be(1);
            atomFeed.AtomEntry.ElementAt(2).Content.Allocation.AllocationStatus.Should().Be("Approved");
            atomFeed.AtomEntry.ElementAt(2).Content.Allocation.AllocationAmount.Should().Be(20);
            atomFeed.AtomEntry.ElementAt(2).Content.Allocation.ProfilePeriods.Length.Should().Be(0);
        }

        [TestMethod]
        public async Task GetNotifications_GivenAcceptHeaderNotSupplied_ReturnsBadRequest()
        {
            //Arrange
            SearchFeed<AllocationNotificationFeedIndex> feeds = new SearchFeed<AllocationNotificationFeedIndex>
            {
                PageRef = 3,
                Top = 500,
                TotalCount = 3,
                Entries = CreateFeedIndexes()
            };

            IAllocationNotificationsFeedsSearchService feedsSearchService = CreateSearchService();
            feedsSearchService
                .GetFeeds(Arg.Is(3), Arg.Is(500), Arg.Any<IEnumerable<string>>())
                .Returns(feeds);

            AllocationNotificationFeedsService service = CreateService(feedsSearchService);

            HttpRequest request = Substitute.For<HttpRequest>();
            request.Scheme.Returns("https");
            request.Path.Returns(new PathString("/api/v1/test/3"));
            request.Host.Returns(new HostString("wherever.naf:12345"));
            request.QueryString.Returns(new QueryString("?allocationStatuses=Published,Approved"));

            //Act
            IActionResult result = await service.GetNotifications(3, "Published,Approved", request);

            //Assert
            result
                .Should()
                .BeOfType<BadRequestResult>();
        }

        static AllocationNotificationFeedsService CreateService(IAllocationNotificationsFeedsSearchService searchService = null)
        {
            return new AllocationNotificationFeedsService(searchService ?? CreateSearchService());
        }

        static IAllocationNotificationsFeedsSearchService CreateSearchService()
        {
            return Substitute.For<IAllocationNotificationsFeedsSearchService>();
        }

        static IEnumerable<AllocationNotificationFeedIndex> CreateFeedIndexes()
        {
            return new[]
                {
                    new AllocationNotificationFeedIndex
                    {
                         AllocationAmount = 10,
                         AllocationLearnerCount = 0,
                         AllocationLineId = "al-1",
                         AllocationLineName = "al 1",
                         AllocationStatus = "Published",
                         AllocationVersionNumber = 1,
                         DateUpdated = DateTimeOffset.Now,
                         FundingPeriodEndDate = DateTimeOffset.Now.AddYears(2),
                         FundingPeriodId = "fp-1",
                         FundingPeriodStartDate = DateTimeOffset.Now.AddYears(-1),
                         FundingPeriodType = "fp type 1",
                         FundingStreamId = "fs-1",
                         FundingStreamName = "fs 1",
                         Id = "id-1",
                         ProviderId = "1111",
                         ProviderUkPrn = "1111",
                         ProviderUpin = "0001",
                         Summary = "test summary 1",
                         Title = "test title 1",
                         ProviderProfiling = "[]"
                    },
                    new AllocationNotificationFeedIndex
                    {
                         AllocationAmount = 100,
                         AllocationLearnerCount = 0,
                         AllocationLineId = "al-2",
                         AllocationLineName = "al 2",
                         AllocationStatus = "Published",
                         AllocationVersionNumber = 1,
                         DateUpdated = DateTimeOffset.Now,
                         FundingPeriodEndDate = DateTimeOffset.Now.AddYears(2),
                         FundingPeriodId = "fp-2",
                         FundingPeriodStartDate = DateTimeOffset.Now.AddYears(-1),
                         FundingPeriodType = "fp type 2",
                         FundingStreamId = "fs-2",
                         FundingStreamName = "fs 2",
                         Id = "id-2",
                         ProviderId = "2222",
                         ProviderUkPrn = "2222",
                         ProviderUpin = "0002",
                         Summary = "test summary 2",
                         Title = "test title 2",
                         ProviderProfiling = "[{\"period\":\"Oct\",\"occurrence\":1,\"periodYear\":2017,\"periodType\":\"CalendarMonth\",\"periodValue\":5.5,\"distributionPeriod\":\"2017-2018\"},{\"period\":\"Apr\",\"occurrence\":1,\"periodYear\":2018,\"periodType\":\"CalendarMonth\",\"periodValue\":5.5,\"distributionPeriod\":\"2017-2018\"}]"
                    },
                    new AllocationNotificationFeedIndex
                    {
                         AllocationAmount = 20,
                         AllocationLearnerCount = 0,
                         AllocationLineId = "al-3",
                         AllocationLineName = "al 3",
                         AllocationStatus = "Approved",
                         AllocationVersionNumber = 1,
                         DateUpdated = DateTimeOffset.Now,
                         FundingPeriodEndDate = DateTimeOffset.Now.AddYears(2),
                         FundingPeriodId = "fp-3",
                         FundingPeriodStartDate = DateTimeOffset.Now.AddYears(-1),
                         FundingPeriodType = "fp type 3",
                         FundingStreamId = "fs-3",
                         FundingStreamName = "fs 3",
                         Id = "id-3",
                         ProviderId = "3333",
                         ProviderUkPrn = "3333",
                         ProviderUpin = "0003",
                         Summary = "test summary 3",
                         Title = "test title 3",
                         ProviderProfiling = "[]"
                    }
                };
        }
    }
}
