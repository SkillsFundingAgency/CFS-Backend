using CalculateFunding.Api.External.V3.Interfaces;
using CalculateFunding.Api.External.V3.Services;
using CalculateFunding.Models.External;
using CalculateFunding.Models.External.V3.AtomItems;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Publishing.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NSubstitute;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CalculateFunding.Api.External.UnitTests.Version3
{
    [TestClass]
    public class FundingFeedServiceTests
    {
        private const int IndexItems = 1000;

        [TestMethod]
        public async Task GetNotifications_GivenPageRefOfThreeSupplied_RequestsPageThree()
        {
            //Arrange
            IFundingFeedSearchService feedsSearchService = CreateSearchService();

            FundingFeedService service = CreateService(feedsSearchService);

            HttpRequest request = Substitute.For<HttpRequest>();
            HttpResponse response = Substitute.For<HttpResponse>();

            //Act
            await service.GetFunding(request, response, pageRef: 3);

            //Assert
            await
            feedsSearchService
                .Received(1)
                .GetFeedsV3(Arg.Is(3), Arg.Is(500));
        }

        [TestMethod]
        public async Task GetNotifications_GivenInvalidPageRef_ReturnsBadRequest()
        {
            //Arrange
            FundingFeedService service = CreateService();

            HttpRequest request = Substitute.For<HttpRequest>();
            HttpResponse response = Substitute.For<HttpResponse>();

            //Act
            IActionResult result = await service.GetFunding(request, response, pageRef: -1);

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
            HttpResponse response = Substitute.For<HttpResponse>();

            //Act
            IActionResult result = await service.GetFunding(request, response, 1, pageSize: 0);

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
            HttpResponse response = Substitute.For<HttpResponse>();

            //Act
            IActionResult result = await service.GetFunding(request, response, 1, pageSize: 1000);

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
            SearchFeedResult<PublishedFundingIndex> feeds = new SearchFeedResult<PublishedFundingIndex>();

            IFundingFeedSearchService feedsSearchService = CreateSearchService();
            feedsSearchService
                .GetFeedsV3(Arg.Is(3), Arg.Is(500))
                .Returns(feeds);

            Mock<IExternalEngineOptions> externalEngineOptions = new Mock<IExternalEngineOptions>();
            externalEngineOptions
                .Setup(_ => _.BlobLookupConcurrencyCount)
                .Returns(10);

            FundingFeedService service = CreateService(feedsSearchService,
                externalEngineOptions: externalEngineOptions.Object);

            HttpRequest request = Substitute.For<HttpRequest>();
            HttpResponse response = Substitute.For<HttpResponse>();

            //Act
            IActionResult result = await service.GetFunding(request, response, pageRef: 3);

            //Assert
            result
                .Should()
                .BeOfType<NotFoundResult>();
        }

        [TestMethod]
        public async Task GetNotifications_GivenSearchFeedReturnsNoResultsForTheGivenPage_ReturnsNotFoundResult()
        {
            //Arrange
            SearchFeedResult<PublishedFundingIndex> feeds = new SearchFeedResult<PublishedFundingIndex>()
            {
                TotalCount = 1000,
                Entries = Enumerable.Empty<PublishedFundingIndex>()
            };

            IFundingFeedSearchService feedsSearchService = CreateSearchService();
            feedsSearchService
                .GetFeedsV3(Arg.Is(3), Arg.Is(500))
                .Returns(feeds);

            Mock<IExternalEngineOptions> externalEngineOptions = new Mock<IExternalEngineOptions>();
            externalEngineOptions
                .Setup(_ => _.BlobLookupConcurrencyCount)
                .Returns(10);

            FundingFeedService service = CreateService(feedsSearchService,
                externalEngineOptions: externalEngineOptions.Object);

            HttpRequest request = Substitute.For<HttpRequest>();
            HttpResponse response = Substitute.For<HttpResponse>();

            //Act
            IActionResult result = await service.GetFunding(request, response, pageRef: 3);

            //Assert
            result
                .Should()
                .BeOfType<NotFoundResult>();
        }

        [TestMethod]
        public async Task GetNotifications_GivenSearchFeedReturnsIncompleteArchiveResultsForTheGivenPage_ReturnsNotFoundResult()
        {
            //Arrange
            List<PublishedFundingIndex> searchFeedEntries = CreateFeedIndexes().ToList();
            SearchFeedResult<PublishedFundingIndex> feeds = new SearchFeedResult<PublishedFundingIndex>()
            {
                TotalCount = 11,
                Entries = searchFeedEntries,
                PageRef = 3,
                Top = 4
            };

            IFundingFeedSearchService feedsSearchService = CreateSearchService();
            feedsSearchService
                .GetFeedsV3(Arg.Is(3), Arg.Is(4))
                .Returns(feeds);

            Mock<IExternalEngineOptions> externalEngineOptions = new Mock<IExternalEngineOptions>();
            externalEngineOptions
                .Setup(_ => _.BlobLookupConcurrencyCount)
                .Returns(10);

            FundingFeedService service = CreateService(feedsSearchService,
                externalEngineOptions: externalEngineOptions.Object);

            HttpRequest request = Substitute.For<HttpRequest>();
            HttpResponse response = Substitute.For<HttpResponse>();

            //Act
            IActionResult result = await service.GetFunding(request, response, pageRef: 3, pageSize: 4);

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
            SearchFeedResult<PublishedFundingIndex> feeds = new SearchFeedResult<PublishedFundingIndex>
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

            Mock<IExternalEngineOptions> externalEngineOptions = new Mock<IExternalEngineOptions>();
            externalEngineOptions
                .Setup(_ => _.BlobLookupConcurrencyCount)
                .Returns(10);

            FundingFeedService service = CreateService(
                searchService: feedsSearchService,
                publishedFundingRetrievalService: publishedFundingRetrievalService,
                externalEngineOptions.Object);

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


            Mock<HttpResponse> response = new Mock<HttpResponse>();
            Mock<PipeWriter> pipeWriter = new Mock<PipeWriter>();

            response.Setup(_ => _.BodyWriter).Returns(pipeWriter.Object);

            List<byte> writtenBytes = new List<byte>();

            pipeWriter.Setup(_ => _.WriteAsync(It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<CancellationToken>()))
                .Callback((ReadOnlyMemory<byte> bytes, CancellationToken cancellationToken) =>
                {
                    writtenBytes.AddRange(bytes.Span.ToArray());
                });

            //Act
            IActionResult result = await service.GetFunding(request, response.Object, pageRef: pageRef, pageSize: pageSize);

            //Assert
            result
                .Should()
                .BeOfType<EmptyResult>();

            string responseContent = Encoding.UTF8.GetString(writtenBytes.ToArray());
            AtomFeed<AtomEntry> atomFeed = JsonConvert.DeserializeObject<AtomFeed<AtomEntry>>(responseContent);

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

            atomFeed.AtomEntry.Count.Should().Be(IndexItems);

            for (int i = 0; i < IndexItems; i++)
            {
                string text = $"id-{i + 1}";
                AtomEntry atomEntry = atomFeed.AtomEntry[i];
                atomEntry.Id.Should().Be($"{scheme}://{host}/api/v3/fundings/byId/{text}");
                atomEntry.Title.Should().Be(text);
                atomEntry.Summary.Should().Be(text);
                atomEntry.Content.Should().NotBeNull();
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
            SearchFeedResult<PublishedFundingIndex> feeds = new SearchFeedResult<PublishedFundingIndex>
            {
                PageRef = 2,
                Top = 2,
                TotalCount = 8,
                Entries = CreateFeedIndexes()
            };

            string fundingFeedDocument = JsonConvert.SerializeObject(new
            {
                FundingStreamId = "PES"
            });

            Mock<IFundingFeedSearchService> feedsSearchService = new Mock<IFundingFeedSearchService>();

            feedsSearchService.Setup(_ => _
                    .GetFeedsV3(It.IsAny<int?>(),
                        It.IsAny<int>(),
                        It.IsAny<IEnumerable<string>>(),
                        It.IsAny<IEnumerable<string>>(),
                        It.IsAny<IEnumerable<string>>(),
                        It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(feeds);

            Mock<IPublishedFundingRetrievalService> fundingRetrievalService = new Mock<IPublishedFundingRetrievalService>();

            fundingRetrievalService.Setup(_ => _.GetFundingFeedDocument(It.IsAny<string>(), false))
                .ReturnsAsync(fundingFeedDocument);

            Mock<IExternalEngineOptions> externalEngineOptions = new Mock<IExternalEngineOptions>();
            externalEngineOptions
                .Setup(_ => _.BlobLookupConcurrencyCount)
                .Returns(10);

            FundingFeedService service = CreateService(feedsSearchService.Object,
                fundingRetrievalService.Object, externalEngineOptions.Object);

            IHeaderDictionary headerDictionary = new HeaderDictionary
            {
                { "Accept", new StringValues("application/json") }
            };

            HttpRequest request = Substitute.For<HttpRequest>();
            request.Scheme.Returns("https");
            request.Path.Returns(new PathString("/api/v3/funding/notifications/2"));
            request.Host.Returns(new HostString("wherever.naf:12345"));
            request.Headers.Returns(headerDictionary);

            Mock<HttpResponse> response = new Mock<HttpResponse>();
            Mock<PipeWriter> pipeWriter = new Mock<PipeWriter>();

            response.Setup(_ => _.BodyWriter).Returns(pipeWriter.Object);

            List<byte> writtenBytes = new List<byte>();

            pipeWriter.Setup(_ => _.WriteAsync(It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<CancellationToken>()))
                .Callback((ReadOnlyMemory<byte> bytes, CancellationToken cancellationToken) =>
                {
                    writtenBytes.AddRange(bytes.Span.ToArray());
                });

            //Act
            IActionResult result = await service.GetFunding(request, response.Object, pageSize: 2, pageRef: 2);

            //Assert
            result
                .Should()
                .BeOfType<EmptyResult>();

            string responseContent = Encoding.UTF8.GetString(writtenBytes.ToArray());

            AtomFeed<AtomEntry> atomFeed = JsonConvert.DeserializeObject<AtomFeed<AtomEntry>>(responseContent);

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
        public async Task GetFunding_GivenGetFundingFeedDocumentReturnsEmpty_ThrowAnException()
        {
            //Arrange
            SearchFeedResult<PublishedFundingIndex> feeds = new SearchFeedResult<PublishedFundingIndex>
            {
                PageRef = 2,
                Top = 2,
                TotalCount = 8,
                Entries = CreateFeedIndexes()
            };

            string fundingFeedDocument = JsonConvert.SerializeObject(new
            {
                FundingStreamId = "PES"
            });

            Mock<IFundingFeedSearchService> feedsSearchService = new Mock<IFundingFeedSearchService>();

            feedsSearchService.Setup(_ => _
                    .GetFeedsV3(It.IsAny<int?>(),
                        It.IsAny<int>(),
                        It.IsAny<IEnumerable<string>>(),
                        It.IsAny<IEnumerable<string>>(),
                        It.IsAny<IEnumerable<string>>(),
                        It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(feeds);

            Mock<IPublishedFundingRetrievalService> fundingRetrievalService = new Mock<IPublishedFundingRetrievalService>();

            fundingRetrievalService.Setup(_ => _.GetFundingFeedDocument(It.IsAny<string>(), false))
                .ReturnsAsync(string.Empty);

            Mock<IExternalEngineOptions> externalEngineOptions = new Mock<IExternalEngineOptions>();
            externalEngineOptions
                .Setup(_ => _.BlobLookupConcurrencyCount)
                .Returns(10);

            FundingFeedService service = CreateService(feedsSearchService.Object,
                fundingRetrievalService.Object, externalEngineOptions.Object);

            IHeaderDictionary headerDictionary = new HeaderDictionary
            {
                { "Accept", new StringValues("application/json") }
            };

            HttpRequest request = Substitute.For<HttpRequest>();
            request.Scheme.Returns("https");
            request.Path.Returns(new PathString("/api/v3/funding/notifications/2"));
            request.Host.Returns(new HostString("wherever.naf:12345"));
            request.Headers.Returns(headerDictionary);

            HttpResponse response = Substitute.For<HttpResponse>();
            MemoryStream responseStream = new MemoryStream();
            response.Body.Returns(responseStream);

            StreamReader responseStreamReader = new StreamReader(responseStream);

            //Act
            IActionResult result = await service.GetFunding(request, response, pageSize: 2, pageRef: 2);

            //Assert
            result
                .Should()
                .BeOfType<InternalServerErrorResult>()
                .Which
                .Value.ToString()
                .Should()
                .Contain("No funding content blob found for document path: path-1");

            responseStreamReader.Close();
            responseStreamReader.Dispose();
        }

        [TestMethod]
        public async Task GetNotifications_GivenFundingStreamId_ThenOnlyFundingStreamRequested()
        {
            //Arrange
            IFundingFeedSearchService feedsSearchService = CreateSearchService();

            Mock<IExternalEngineOptions> externalEngineOptions = new Mock<IExternalEngineOptions>();
            externalEngineOptions
                .Setup(_ => _.BlobLookupConcurrencyCount)
                .Returns(10);

            FundingFeedService service = CreateService(feedsSearchService,
                externalEngineOptions: externalEngineOptions.Object);

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

            HttpResponse response = Substitute.For<HttpResponse>();

            //Act
            IActionResult result = await service.GetFunding(request, response, pageRef: 1, pageSize: 2, fundingStreamIds: new[] { "10070703" });

            //Assert
            await feedsSearchService
                .Received(1)
                .GetFeedsV3(Arg.Is(1), Arg.Is(2),
                Arg.Is<IEnumerable<string>>(s => s.Count() == 1 && s.Contains("10070703")),
                Arg.Any<IEnumerable<string>>(), Arg.Any<IEnumerable<string>>());

        }

        private static FundingFeedService CreateService(
            IFundingFeedSearchService searchService = null,
            IPublishedFundingRetrievalService publishedFundingRetrievalService = null,
            IExternalEngineOptions externalEngineOptions = null,
            ILogger logger = null)
        {
            return new FundingFeedService(
                searchService ?? CreateSearchService(),
                publishedFundingRetrievalService ?? CreatePublishedFundingRetrievalService(),
                externalEngineOptions ?? CreateExternalEngineOptions(),
                logger ?? CreateLogger());
        }

        private static IExternalEngineOptions CreateExternalEngineOptions()
        {
            return Substitute.For<IExternalEngineOptions>();
        }

        private static IFundingFeedSearchService CreateSearchService()
        {
            return Substitute.For<IFundingFeedSearchService>();
        }

        private static ILogger CreateLogger()
        {
            return Substitute.For<ILogger>();
        }

        private static IPublishedFundingRetrievalService CreatePublishedFundingRetrievalService()
        {
            return Substitute.For<IPublishedFundingRetrievalService>();
        }

        private static IEnumerable<PublishedFundingIndex> CreateFeedIndexes()
        {
            List<PublishedFundingIndex> indexes = new List<PublishedFundingIndex>();

            for (int i = 1; i <= IndexItems; i++)
            {
                indexes.Add(new PublishedFundingIndex
                {
                    Id = $"id-{i}",
                    FundingStreamId = $"PES-{i}",
                    FundingPeriodId = $"ABC-{i}",
                    GroupTypeIdentifier = "LocalAuthority",
                    StatusChangedDate = DateTime.Now.AddDays(-1),
                    DocumentPath = $"path-{i}"
                });
            }

            return indexes.ToArray();
        }
    }
}
