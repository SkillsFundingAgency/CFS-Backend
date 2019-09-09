using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Api.External.V3.Interfaces;
using CalculateFunding.Api.External.V3.Services;
using CalculateFunding.Models.External.V3.AtomItems;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Models.Search;
using CalculateFunding.Services.Publising.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NSubstitute;

namespace CalculateFunding.Api.External.UnitTests.Version3
{
    [TestClass]
    public class FundingFeedServiceTests
    {
        [TestMethod]
        public async Task GetNotifications_GivenPageRefOfThreeSupplied_RequestsPageThree()
        {
            //Arrange
            IFundingFeedSearchService feedsSearchService = CreateSearchService();

            FundingFeedService service = CreateService(feedsSearchService);

            HttpRequest request = Substitute.For<HttpRequest>();

            //Act
            IActionResult result = await service.GetFunding(request, pageRef: 3);

            //Assert
            await
            feedsSearchService
                .Received(1)
                .GetFeedsV3(Arg.Is(3), Arg.Is(500));
        }

        [TestMethod]
        public async Task GetNotifications_GivenInvallidPageRef_ReturnsBadRequest()
        {
            //Arrange
            FundingFeedService service = CreateService();

            HttpRequest request = Substitute.For<HttpRequest>();

            //Act
            IActionResult result = await service.GetFunding(request, pageRef: -1);

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
        public async Task GetNotifications_GivenInvalidPageSizeOfZero_ReturnsBadRequest()
        {
            //Arrange
            FundingFeedService service = CreateService();

            HttpRequest request = Substitute.For<HttpRequest>();

            //Act
            IActionResult result = await service.GetFunding(request, 1, pageSize: 0);

            //Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>()
                .Which
                .Value
                .Should()
                .Be("Page size should be more that zero and less than or equal to 500");
        }

        [TestMethod]
        public async Task GetNotifications_GivenInvalidPageSizeOfThousand_ReturnsBadRequest()
        {
            //Arrange
            FundingFeedService service = CreateService();

            HttpRequest request = Substitute.For<HttpRequest>();

            //Act
            IActionResult result = await service.GetFunding(request, 1, pageSize: 1000);

            //Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>()
                .Which
                .Value
                .Should()
                .Be("Page size should be more that zero and less than or equal to 500");
        }

        [TestMethod]
        public async Task GetNotifications_GivenSearchFeedReturnsZeroTotalCountResult_ReturnsNotFoundResult()
        {
            //Arrange
            SearchFeedV3<PublishedFundingIndex> feeds = new SearchFeedV3<PublishedFundingIndex>();

            IFundingFeedSearchService feedsSearchService = CreateSearchService();
            feedsSearchService
                .GetFeedsV3(Arg.Is(3), Arg.Is(500))
                .Returns(feeds);

            FundingFeedService service = CreateService(feedsSearchService);

            HttpRequest request = Substitute.For<HttpRequest>();

            //Act
            IActionResult result = await service.GetFunding(request, pageRef: 3);

            //Assert
            result
                .Should()
                .BeOfType<NotFoundResult>();
        }

        [TestMethod]
        public async Task GetNotifications_GivenSearchFeedReturnsNoResultsForTheGivenPage_ReturnsNotFoundResult()
        {
            //Arrange
            SearchFeedV3<PublishedFundingIndex> feeds = new SearchFeedV3<PublishedFundingIndex>()
            {
                TotalCount = 1000,
                Entries = Enumerable.Empty<PublishedFundingIndex>()
            };

            IFundingFeedSearchService feedsSearchService = CreateSearchService();
            feedsSearchService
                .GetFeedsV3(Arg.Is(3), Arg.Is(500))
                .Returns(feeds);

            FundingFeedService service = CreateService(feedsSearchService);

            HttpRequest request = Substitute.For<HttpRequest>();

            //Act
            IActionResult result = await service.GetFunding(request, pageRef: 3);

            //Assert
            result
                .Should()
                .BeOfType<NotFoundResult>();
        }

        [TestMethod]
        public async Task GetNotifications_GivenAQueryStringForWhichThereAreResults_ReturnsAtomFeedWithCorrectLinks()
        {
            //Arrange
            string fundingFeedDocument = JsonConvert.SerializeObject(new
            {
                FundingStreamId = "PES"
            });

            int pageRef = 2;
            int pageSize = 3;

            List<PublishedFundingIndex> searchFeedEntries = CreateFeedIndexes().ToList();
            SearchFeedV3<PublishedFundingIndex> feeds = new SearchFeedV3<PublishedFundingIndex>
            {
                PageRef = pageRef,
                Top = 2,
                TotalCount = 8,
                Entries = searchFeedEntries
            };
            PublishedFundingIndex firstFeedItem = feeds.Entries.ElementAt(0);

            IFundingFeedSearchService feedsSearchService = CreateSearchService();
            feedsSearchService
                .GetFeedsV3(Arg.Is(pageRef), Arg.Is(pageSize))
                .ReturnsForAnyArgs(feeds);

            IPublishedFundingRetrievalService publishedFundingRetrievalService = Substitute.For<IPublishedFundingRetrievalService>();
            publishedFundingRetrievalService
                .GetFundingFeedDocument(Arg.Any<string>())
                .Returns(fundingFeedDocument);

            FundingFeedService service = CreateService(
                searchService: feedsSearchService,
                publishedFundingRetrievalService: publishedFundingRetrievalService);

            IHeaderDictionary headerDictionary = new HeaderDictionary
            {
                { "Accept", new StringValues("application/json") }
            };

            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "pageRef", new StringValues(pageRef.ToString()) },
                { "allocationStatuses", new StringValues("Published,Approved") },
                { "pageSize", new StringValues(pageSize.ToString()) }
            });

            string scheme = "https";
            string path = "/api/v3/fundings/notifications";
            string host = "wherever.naf:12345";
            string queryString = "?pageSize=2";

            HttpRequest request = Substitute.For<HttpRequest>();
            request.Scheme.Returns(scheme);
            request.Path.Returns(new PathString(path));
            request.Host.Returns(new HostString(host));
            request.QueryString.Returns(new QueryString(queryString));
            request.Headers.Returns(headerDictionary);
            request.Query.Returns(queryStringValues);

            //Act
            IActionResult result = await service.GetFunding(request, pageRef: pageRef, pageSize: pageSize);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();

            OkObjectResult contentResult = result as OkObjectResult;
            AtomFeed<AtomEntry> atomFeed = contentResult.Value as AtomFeed<AtomEntry>;

            atomFeed
                .Should()
                .NotBeNull();

            atomFeed.Id.Should().NotBeEmpty();
            atomFeed.Title.Should().Be("Calculate Funding Service Funding Feed");
            atomFeed.Author.Name.Should().Be("Calculate Funding Service");
            atomFeed.Link.First(m => m.Rel == "next-archive").Href.Should().Be($"{scheme}://{host}{path}/3{queryString}");
            atomFeed.Link.First(m => m.Rel == "prev-archive").Href.Should().Be($"{scheme}://{host}{path}/1{queryString}");
            atomFeed.Link.First(m => m.Rel == "self").Href.Should().Be($"{scheme}://{host}{path}{queryString}");
            atomFeed.Link.First(m => m.Rel == "current").Href.Should().Be($"{scheme}://{host}{path}/2{queryString}");

            atomFeed.AtomEntry.Count.Should().Be(3);

            for (int i = 0; i < 3; i++)
            {
                string text = $"id-{i + 1}";
                atomFeed.AtomEntry.ElementAt(i).Id.Should().Be($"{scheme}://{host}/api/v3/fundings/byId/{text}");
                atomFeed.AtomEntry.ElementAt(i).Title.Should().Be(text);
                atomFeed.AtomEntry.ElementAt(i).Summary.Should().Be(text);
                atomFeed.AtomEntry.ElementAt(i).Content.Should().NotBeNull();
            }

            JObject content = atomFeed.AtomEntry.ElementAt(0).Content as JObject;
            content.TryGetValue("FundingStreamId", out JToken token);
            ((JValue)token).Value<string>().Should().Be("PES");

            await feedsSearchService
                .Received(1)
                .GetFeedsV3(pageRef, pageSize, null, null, null);

            await publishedFundingRetrievalService
                .Received(searchFeedEntries.Count)
                .GetFundingFeedDocument(Arg.Any<string>());

            foreach (PublishedFundingIndex index in searchFeedEntries)
            {
                await publishedFundingRetrievalService
                    .Received(1)
                    .GetFundingFeedDocument(index.DocumentPath);
            }
        }

        [TestMethod]
        public async Task GetNotifications_GivenSearchFeedReturnsResultsWhenNoQueryParameters_EnsuresAtomLinksCorrect()
        {
            //Arrange
            SearchFeedV3<PublishedFundingIndex> feeds = new SearchFeedV3<PublishedFundingIndex>
            {
                PageRef = 2,
                Top = 2,
                TotalCount = 8,
                Entries = CreateFeedIndexes()
            };

            IFundingFeedSearchService feedsSearchService = CreateSearchService();
            feedsSearchService
                .GetFeedsV3(Arg.Is(2), Arg.Is(2))
                .ReturnsForAnyArgs(feeds);

            FundingFeedService service = CreateService(feedsSearchService);

            IHeaderDictionary headerDictionary = new HeaderDictionary();
            headerDictionary.Add("Accept", new StringValues("application/json"));

            HttpRequest request = Substitute.For<HttpRequest>();
            request.Scheme.Returns("https");
            request.Path.Returns(new PathString("/api/v3/funding/notifications/2"));
            request.Host.Returns(new HostString("wherever.naf:12345"));
            request.Headers.Returns(headerDictionary);

            //Act
            IActionResult result = await service.GetFunding(request, pageSize: 2, pageRef: 2);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();

            OkObjectResult contentResult = result as OkObjectResult;
            AtomFeed<AtomEntry> atomFeed = contentResult.Value as AtomFeed<AtomEntry>;

            atomFeed
                .Should()
                .NotBeNull();

            atomFeed.Id.Should().NotBeEmpty();
            atomFeed.Title.Should().Be("Calculate Funding Service Funding Feed");
            atomFeed.Author.Name.Should().Be("Calculate Funding Service");
            atomFeed.Link.First(m => m.Rel == "prev-archive").Href.Should().Be("https://wherever.naf:12345/api/v3/funding/notifications/1");
            atomFeed.Link.First(m => m.Rel == "next-archive").Href.Should().Be("https://wherever.naf:12345/api/v3/funding/notifications/3");
            atomFeed.Link.First(m => m.Rel == "current").Href.Should().Be("https://wherever.naf:12345/api/v3/funding/notifications/2");
            atomFeed.Link.First(m => m.Rel == "self").Href.Should().Be("https://wherever.naf:12345/api/v3/funding/notifications");
        }

        [TestMethod]
        public async Task GetNotifications_GivenFundingStreamId_ThenOnlyFundingStreamRequested()
        {
            //Arrange
            IFundingFeedSearchService feedsSearchService = CreateSearchService();

            FundingFeedService service = CreateService(feedsSearchService);

            IHeaderDictionary headerDictionary = new HeaderDictionary
            {
                { "Accept", new StringValues("application/json") }
            };

            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "pageRef", new StringValues("1") },
                { "ukprn", new StringValues("10070703") },
                { "pageSize", new StringValues("2") }
            });

            HttpRequest request = Substitute.For<HttpRequest>();
            request.Scheme.Returns("https");
            request.Path.Returns(new PathString("/api/v3/test"));
            request.Host.Returns(new HostString("wherever.naf:12345"));
            request.QueryString.Returns(new QueryString("?pageRef=1&pageSize=2&ukprn=10070703"));
            request.Headers.Returns(headerDictionary);
            request.Query.Returns(queryStringValues);

            //Act
            IActionResult result = await service.GetFunding(request, pageRef: 1, pageSize: 2, fundingStreamIds: new[] { "10070703" });

            //Assert
            await feedsSearchService
                .Received(1)
                .GetFeedsV3(Arg.Is(1), Arg.Is(2),
                Arg.Is<IEnumerable<string>>(s => s.Count() == 1 && s.Contains("10070703")),
                Arg.Any<IEnumerable<string>>(), Arg.Any<IEnumerable<string>>());

        }

        private static FundingFeedService CreateService(
            IFundingFeedSearchService searchService = null,
            IPublishedFundingRetrievalService publishedFundingRetrievalService = null)
        {
            return new FundingFeedService(
                searchService ?? CreateSearchService(),
                publishedFundingRetrievalService ?? CreatePublishedFundingRetrievalService()
                );
        }

        private static IFundingFeedSearchService CreateSearchService()
        {
            return Substitute.For<IFundingFeedSearchService>();
        }

        private static IPublishedFundingRetrievalService CreatePublishedFundingRetrievalService()
        {
            return Substitute.For<IPublishedFundingRetrievalService>();
        }

        private static IEnumerable<PublishedFundingIndex> CreateFeedIndexes()
        {
            return new[]
                {
                    new PublishedFundingIndex
                    {
                         Id = "id-1",
                         FundingStreamId = "PES",
                         FundingPeriodId = "ABC",
                         GroupTypeIdentifier = "LocalAuthority",
                         StatusChangedDate = DateTime.Now.AddDays(-1),
                         DocumentPath = "a"
                    },
                    new PublishedFundingIndex
                    {
                         Id = "id-2",
                         FundingStreamId = "PES1",
                         FundingPeriodId = "ABC1",
                         GroupTypeIdentifier = "LocalAuthority",
                         StatusChangedDate = DateTime.Now.AddDays(-2),
                         DocumentPath = "b"
                    },
                    new PublishedFundingIndex
                    {
                         Id = "id-3",
                         FundingStreamId = "PES2",
                         FundingPeriodId = "ABC2",
                         GroupTypeIdentifier = "LocalAuthority",
                         StatusChangedDate = DateTime.Now.AddDays(-3),
                         DocumentPath = "c"
                    }
                };
        }
    }
}
