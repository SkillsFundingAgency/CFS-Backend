using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;
using NSubstitute;
using CalculateFunding.Services.Results.Interfaces;
using CalculateFunding.Api.External.V1.Services;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using FluentAssertions;
using System.Linq;
using CalculateFunding.Models.Results;
using CalculateFunding.Models.Search;
using Microsoft.Extensions.Primitives;
using Serilog;
using CalculateFunding.Api.External.V1.Models;
using Newtonsoft.Json;

namespace CalculateFunding.Api.External.UnitTests.Services
{
    [TestClass]
    public class ProviderResultsServiceTests
    {
        private const string providerId = "123456";
        private const int startYear = 2018;
        private const int endYear = 2019;
        private const string allocationLineIds = "AllocationLine1";
        private const string fundingStreamIds = "FundingStream1";
        private const string laCode = "123";

        [TestMethod]
        public async Task GetProviderResultsForAllocations_GivenMissingProviderId_ReturnsBadRequest()
        {
            //Arrange
            HttpRequest httpRequest = Substitute.For<HttpRequest>();

            ProviderResultsService providerResultsService = CreateService();

            //Act
            IActionResult result = await providerResultsService.GetProviderResultsForAllocations(null, startYear, endYear, allocationLineIds, httpRequest);

            //Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>()
                .Which
                .Value
                .Should()
                .Be("Missing providerId");
        }

        [TestMethod]
        public async Task GetProviderResultsForAllocations_GivenMissingStartYear_ReturnsBadRequest()
        {
            //Arrange
            HttpRequest httpRequest = Substitute.For<HttpRequest>();

            ProviderResultsService providerResultsService = CreateService();

            //Act
            IActionResult result = await providerResultsService.GetProviderResultsForAllocations(providerId, 0, endYear, allocationLineIds, httpRequest);

            //Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>()
                .Which
                .Value
                .Should()
                .Be("Missing start year");
        }

        [TestMethod]
        public async Task GetProviderResultsForAllocations_GivenMissingEndYear_ReturnsBadRequest()
        {
            //Arrange
            HttpRequest httpRequest = Substitute.For<HttpRequest>();

            ProviderResultsService providerResultsService = CreateService();

            //Act
            IActionResult result = await providerResultsService.GetProviderResultsForAllocations(providerId, startYear, 0, allocationLineIds, httpRequest);

            //Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>()
                .Which
                .Value
                .Should()
                .Be("Missing end year");
        }

        [TestMethod]
        public async Task GetProviderResultsForAllocations_GivenMissingAllocationLineIds_ReturnsBadRequest()
        {
            //Arrange
            HttpRequest httpRequest = Substitute.For<HttpRequest>();

            ProviderResultsService providerResultsService = CreateService();

            //Act
            IActionResult result = await providerResultsService.GetProviderResultsForAllocations(providerId, startYear, endYear, null, httpRequest);

            //Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>()
                .Which
                .Value
                .Should()
                .Be("Missing allocation line ids");
        }

        [TestMethod]
        public async Task GetProviderResultsForAllocations_GivenNoResultsFoundFromSearch_ReturnsNotFoundResult()
        {
            //Arrange
            HttpRequest httpRequest = Substitute.For<HttpRequest>();

            IAllocationNotificationsFeedsSearchService searchService = CreateSearchService();

            ProviderResultsService providerResultsService = CreateService(searchService);

            //Act
            IActionResult result = await providerResultsService.GetProviderResultsForAllocations(providerId, startYear, endYear, allocationLineIds, httpRequest);

            //Assert
            result
                .Should()
                .BeOfType<NotFoundResult>();

            await
                searchService
                    .Received(1)
                    .GetFeeds(Arg.Is(providerId), Arg.Is(startYear), Arg.Is(endYear), Arg.Is<IList<string>>(m => m.Count == 1 && m.First() == "allocationLineId eq 'AllocationLine1'"));
        }

        [TestMethod]
        public async Task GetProviderResultsForAllocations_GivenSearchResultHasMissingProviderId_ReturnsNotFoundResult()
        {
            //Arrange
            HttpRequest httpRequest = Substitute.For<HttpRequest>();

            AllocationNotificationFeedIndex allocationNotificationFeedIndex = CreateFeedIndexes().First();

            allocationNotificationFeedIndex.ProviderId = null;

            SearchFeed<AllocationNotificationFeedIndex> feeds = new SearchFeed<AllocationNotificationFeedIndex>
            {
                Entries = new[] { allocationNotificationFeedIndex }
            };

            IAllocationNotificationsFeedsSearchService searchService = CreateSearchService();
            searchService
                .GetFeeds(Arg.Is(providerId), Arg.Is(startYear), Arg.Is(endYear), Arg.Any<IEnumerable<string>>())
                .Returns(feeds);

            ILogger logger = CreateLogger();

            ProviderResultsService providerResultsService = CreateService(searchService, logger);

            //Act
            IActionResult result = await providerResultsService.GetProviderResultsForAllocations(providerId, startYear, endYear, allocationLineIds, httpRequest);

            //Assert
            result
                .Should()
                .BeOfType<NotFoundResult>();

            logger
                .Received(1)
                .Warning(Arg.Is($"Feed with id {allocationNotificationFeedIndex.Id} contains missing data: provider id"));
        }

        [TestMethod]
        public async Task GetProviderResultsForAllocations_GivenSearchResultHasMissingFundingPeriodId_ReturnsNotFoundResult()
        {
            //Arrange
            HttpRequest httpRequest = Substitute.For<HttpRequest>();

            AllocationNotificationFeedIndex allocationNotificationFeedIndex = CreateFeedIndexes().First();

            allocationNotificationFeedIndex.FundingPeriodId = null;

            SearchFeed<AllocationNotificationFeedIndex> feeds = new SearchFeed<AllocationNotificationFeedIndex>
            {
                Entries = new[] { allocationNotificationFeedIndex }
            };

            IAllocationNotificationsFeedsSearchService searchService = CreateSearchService();
            searchService
                .GetFeeds(Arg.Is(providerId), Arg.Is(startYear), Arg.Is(endYear), Arg.Any<IEnumerable<string>>())
                .Returns(feeds);

            ILogger logger = CreateLogger();

            ProviderResultsService providerResultsService = CreateService(searchService, logger);

            //Act
            IActionResult result = await providerResultsService.GetProviderResultsForAllocations(providerId, startYear, endYear, allocationLineIds, httpRequest);

            //Assert
            result
                .Should()
                .BeOfType<NotFoundResult>();

            logger
                .Received(1)
                .Warning(Arg.Is($"Feed with id {allocationNotificationFeedIndex.Id} contains missing data: funding period id or type"));
        }

        [TestMethod]
        public async Task GetProviderResultsForAllocations_GivenSearchResultHasMissingFundingPeriodType_ReturnsNotFoundResult()
        {
            //Arrange
            HttpRequest httpRequest = Substitute.For<HttpRequest>();

            AllocationNotificationFeedIndex allocationNotificationFeedIndex = CreateFeedIndexes().First();

            allocationNotificationFeedIndex.FundingPeriodType = null;

            SearchFeed<AllocationNotificationFeedIndex> feeds = new SearchFeed<AllocationNotificationFeedIndex>
            {
                Entries = new[] { allocationNotificationFeedIndex }
            };

            IAllocationNotificationsFeedsSearchService searchService = CreateSearchService();
            searchService
                .GetFeeds(Arg.Is(providerId), Arg.Is(startYear), Arg.Is(endYear), Arg.Any<IEnumerable<string>>())
                .Returns(feeds);

            ILogger logger = CreateLogger();

            ProviderResultsService providerResultsService = CreateService(searchService, logger);

            //Act
            IActionResult result = await providerResultsService.GetProviderResultsForAllocations(providerId, startYear, endYear, allocationLineIds, httpRequest);

            //Assert
            result
                .Should()
                .BeOfType<NotFoundResult>();

            logger
                .Received(1)
                .Warning(Arg.Is($"Feed with id {allocationNotificationFeedIndex.Id} contains missing data: funding period id or type"));
        }

        [TestMethod]
        public async Task GetProviderResultsForAllocations_GivenSearchResultHasMissingFundingStreamId_ReturnsNotFoundResult()
        {
            //Arrange
            HttpRequest httpRequest = Substitute.For<HttpRequest>();

            AllocationNotificationFeedIndex allocationNotificationFeedIndex = CreateFeedIndexes().First();

            allocationNotificationFeedIndex.FundingStreamId = null;

            SearchFeed<AllocationNotificationFeedIndex> feeds = new SearchFeed<AllocationNotificationFeedIndex>
            {
                Entries = new[] { allocationNotificationFeedIndex }
            };

            IAllocationNotificationsFeedsSearchService searchService = CreateSearchService();
            searchService
                .GetFeeds(Arg.Is(providerId), Arg.Is(startYear), Arg.Is(endYear), Arg.Any<IEnumerable<string>>())
                .Returns(feeds);

            ILogger logger = CreateLogger();

            ProviderResultsService providerResultsService = CreateService(searchService, logger);

            //Act
            IActionResult result = await providerResultsService.GetProviderResultsForAllocations(providerId, startYear, endYear, allocationLineIds, httpRequest);

            //Assert
            result
                .Should()
                .BeOfType<NotFoundResult>();

            logger
                .Received(1)
                .Warning(Arg.Is($"Feed with id {allocationNotificationFeedIndex.Id} contains missing data: funding stream id or name"));
        }

        [TestMethod]
        public async Task GetProviderResultsForAllocations_GivenSearchResultHasMissingFundingStreamName_ReturnsNotFoundResult()
        {
            //Arrange
            HttpRequest httpRequest = Substitute.For<HttpRequest>();

            AllocationNotificationFeedIndex allocationNotificationFeedIndex = CreateFeedIndexes().First();

            allocationNotificationFeedIndex.FundingStreamName = null;

            SearchFeed<AllocationNotificationFeedIndex> feeds = new SearchFeed<AllocationNotificationFeedIndex>
            {
                Entries = new[] { allocationNotificationFeedIndex }
            };

            IAllocationNotificationsFeedsSearchService searchService = CreateSearchService();
            searchService
                .GetFeeds(Arg.Is(providerId), Arg.Is(startYear), Arg.Is(endYear), Arg.Any<IEnumerable<string>>())
                .Returns(feeds);

            ILogger logger = CreateLogger();

            ProviderResultsService providerResultsService = CreateService(searchService, logger);

            //Act
            IActionResult result = await providerResultsService.GetProviderResultsForAllocations(providerId, startYear, endYear, allocationLineIds, httpRequest);

            //Assert
            result
                .Should()
                .BeOfType<NotFoundResult>();

            logger
                .Received(1)
                .Warning(Arg.Is($"Feed with id {allocationNotificationFeedIndex.Id} contains missing data: funding stream id or name"));
        }

        [TestMethod]
        public async Task GetProviderResultsForAllocations_GivenSearchResultHasMiddingAllocationLineId_ReturnsNotFoundResult()
        {
            //Arrange
            HttpRequest httpRequest = Substitute.For<HttpRequest>();

            AllocationNotificationFeedIndex allocationNotificationFeedIndex = CreateFeedIndexes().First();

            allocationNotificationFeedIndex.AllocationLineId = null;

            SearchFeed<AllocationNotificationFeedIndex> feeds = new SearchFeed<AllocationNotificationFeedIndex>
            {
                Entries = new[] { allocationNotificationFeedIndex }
            };

            IAllocationNotificationsFeedsSearchService searchService = CreateSearchService();
            searchService
                .GetFeeds(Arg.Is(providerId), Arg.Is(startYear), Arg.Is(endYear), Arg.Any<IEnumerable<string>>())
                .Returns(feeds);

            ILogger logger = CreateLogger();

            ProviderResultsService providerResultsService = CreateService(searchService, logger);

            //Act
            IActionResult result = await providerResultsService.GetProviderResultsForAllocations(providerId, startYear, endYear, allocationLineIds, httpRequest);

            //Assert
            result
                .Should()
                .BeOfType<NotFoundResult>();

            logger
                .Received(1)
                .Warning(Arg.Is($"Feed with id {allocationNotificationFeedIndex.Id} contains missing data: allocation line id or name"));
        }

        [TestMethod]
        public async Task GetProviderResultsForAllocations_GivenSearchResultHasMissingAllocationLineName_ReturnsNotFoundResult()
        {
            //Arrange
            HttpRequest httpRequest = Substitute.For<HttpRequest>();

            AllocationNotificationFeedIndex allocationNotificationFeedIndex = CreateFeedIndexes().First();

            allocationNotificationFeedIndex.AllocationLineName = null;

            SearchFeed<AllocationNotificationFeedIndex> feeds = new SearchFeed<AllocationNotificationFeedIndex>
            {
                Entries = new[] { allocationNotificationFeedIndex }
            };

            IAllocationNotificationsFeedsSearchService searchService = CreateSearchService();
            searchService
                .GetFeeds(Arg.Is(providerId), Arg.Is(startYear), Arg.Is(endYear), Arg.Any<IEnumerable<string>>())
                .Returns(feeds);

            ILogger logger = CreateLogger();

            ProviderResultsService providerResultsService = CreateService(searchService, logger);

            //Act
            IActionResult result = await providerResultsService.GetProviderResultsForAllocations(providerId, startYear, endYear, allocationLineIds, httpRequest);

            //Assert
            result
                .Should()
                .BeOfType<NotFoundResult>();

            logger
                .Received(1)
                .Warning(Arg.Is($"Feed with id {allocationNotificationFeedIndex.Id} contains missing data: allocation line id or name"));
        }

        [TestMethod]
        public async Task GetProviderResultsForAllocations_GivenSearchResultHasEmptyPolicies_ReturnsNotFoundResult()
        {
            //Arrange
            HttpRequest httpRequest = Substitute.For<HttpRequest>();

            AllocationNotificationFeedIndex allocationNotificationFeedIndex = CreateFeedIndexes().First();

            allocationNotificationFeedIndex.PolicySummaries = "[]";

            SearchFeed<AllocationNotificationFeedIndex> feeds = new SearchFeed<AllocationNotificationFeedIndex>
            {
                Entries = new[] { allocationNotificationFeedIndex }
            };

            IAllocationNotificationsFeedsSearchService searchService = CreateSearchService();
            searchService
                .GetFeeds(Arg.Is(providerId), Arg.Is(startYear), Arg.Is(endYear), Arg.Any<IEnumerable<string>>())
                .Returns(feeds);

            ILogger logger = CreateLogger();

            ProviderResultsService providerResultsService = CreateService(searchService, logger);

            //Act
            IActionResult result = await providerResultsService.GetProviderResultsForAllocations(providerId, startYear, endYear, allocationLineIds, httpRequest);

            //Assert
            result
                .Should()
                .BeOfType<NotFoundResult>();

            logger
                .Received(1)
                .Warning(Arg.Is($"Feed with id {allocationNotificationFeedIndex.Id} contains missing data: policy summaries"));
        }

        [TestMethod]
        public async Task GetProviderResultsForAllocations_GivenSearchResultHasNullPolicies_ReturnsNotFoundResult()
        {
            //Arrange
            HttpRequest httpRequest = Substitute.For<HttpRequest>();

            AllocationNotificationFeedIndex allocationNotificationFeedIndex = CreateFeedIndexes().First();

            allocationNotificationFeedIndex.PolicySummaries = null;

            SearchFeed<AllocationNotificationFeedIndex> feeds = new SearchFeed<AllocationNotificationFeedIndex>
            {
                Entries = new[] { allocationNotificationFeedIndex }
            };

            IAllocationNotificationsFeedsSearchService searchService = CreateSearchService();
            searchService
                .GetFeeds(Arg.Is(providerId), Arg.Is(startYear), Arg.Is(endYear), Arg.Any<IEnumerable<string>>())
                .Returns(feeds);

            ILogger logger = CreateLogger();

            ProviderResultsService providerResultsService = CreateService(searchService, logger);

            //Act
            IActionResult result = await providerResultsService.GetProviderResultsForAllocations(providerId, startYear, endYear, allocationLineIds, httpRequest);

            //Assert
            result
                .Should()
                .BeOfType<NotFoundResult>();

            logger
                .Received(1)
                .Warning(Arg.Is($"Feed with id {allocationNotificationFeedIndex.Id} contains missing data: policy summaries"));
        }

        [TestMethod]
        public async Task GetProviderResultsForAllocations_GivenSearchResultHasInvalidPolicies_ReturnsNotFoundResult()
        {
            //Arrange
            HttpRequest httpRequest = Substitute.For<HttpRequest>();

            AllocationNotificationFeedIndex allocationNotificationFeedIndex = CreateFeedIndexes().First();

            allocationNotificationFeedIndex.PolicySummaries = "whatever";

            SearchFeed<AllocationNotificationFeedIndex> feeds = new SearchFeed<AllocationNotificationFeedIndex>
            {
                Entries = new[] { allocationNotificationFeedIndex }
            };

            IAllocationNotificationsFeedsSearchService searchService = CreateSearchService();
            searchService
                .GetFeeds(Arg.Is(providerId), Arg.Is(startYear), Arg.Is(endYear), Arg.Any<IEnumerable<string>>())
                .Returns(feeds);

            ILogger logger = CreateLogger();

            ProviderResultsService providerResultsService = CreateService(searchService, logger);

            //Act
            IActionResult result = await providerResultsService.GetProviderResultsForAllocations(providerId, startYear, endYear, allocationLineIds, httpRequest);

            //Assert
            result
                .Should()
                .BeOfType<NotFoundResult>();

            logger
                .Received(1)
                .Error(Arg.Any<Exception>(), Arg.Is($"Failed to deserialize policies for feed id {allocationNotificationFeedIndex.Id}"));
        }

        [TestMethod]
        public async Task GetProviderResultsForAllocations_GivenValidResultsFoundFromSearch_ReturnsResults()
        {
            //Arrange
            IHeaderDictionary headerDictionary = new HeaderDictionary();
            headerDictionary.Add("Accept", new StringValues("application/json"));

            HttpRequest httpRequest = Substitute.For<HttpRequest>();
            httpRequest.Headers.Returns(headerDictionary);

            SearchFeed<AllocationNotificationFeedIndex> feeds = new SearchFeed<AllocationNotificationFeedIndex>
            {
                Entries = CreateFeedIndexes()
            };

            IAllocationNotificationsFeedsSearchService searchService = CreateSearchService();
            searchService
                .GetFeeds(Arg.Is(providerId), Arg.Is(startYear), Arg.Is(endYear), Arg.Any<IEnumerable<string>>())
                .Returns(feeds);

            ProviderResultsService providerResultsService = CreateService(searchService);

            //Act
            IActionResult result = await providerResultsService.GetProviderResultsForAllocations(providerId, startYear, endYear, allocationLineIds, httpRequest);

            //Assert
            result
                .Should()
                .BeOfType<ContentResult>()
                .Which
                .StatusCode
                .Should()
                .Be(200);

            ContentResult contentResult = result as ContentResult;

            ProviderResultSummary providerResultSummary = JsonConvert.DeserializeObject<ProviderResultSummary>(contentResult.Content);

            providerResultSummary.TotalAmount.Should().Be(130);
            providerResultSummary.Provider.Ukprn.Should().Be("1111");
            providerResultSummary.FundingPeriodResults.Count().Should().Be(3);
            providerResultSummary.FundingPeriodResults.ElementAt(0).FundingStreamResults.Count().Should().Be(1);
            providerResultSummary.FundingPeriodResults.ElementAt(1).FundingStreamResults.Count().Should().Be(1);
            providerResultSummary.FundingPeriodResults.ElementAt(2).FundingStreamResults.Count().Should().Be(1);
            providerResultSummary.FundingPeriodResults.ElementAt(0).FundingStreamResults.First().FundingStream.FundingStreamCode.Should().Be("fs-1");
            providerResultSummary.FundingPeriodResults.ElementAt(0).FundingStreamResults.First().FundingStream.FundingStreamName.Should().Be("fs 1");
            providerResultSummary.FundingPeriodResults.ElementAt(0).FundingStreamResults.First().TotalAmount = 10;
            providerResultSummary.FundingPeriodResults.ElementAt(0).FundingStreamResults.First().Allocations.Count().Should().Be(1);
            providerResultSummary.FundingPeriodResults.ElementAt(0).FundingStreamResults.First().Allocations.First().AllocationLine.AllocationLineCode.Should().Be("al-1");
            providerResultSummary.FundingPeriodResults.ElementAt(0).FundingStreamResults.First().Allocations.First().AllocationLine.AllocationLineName.Should().Be("al 1");
            providerResultSummary.FundingPeriodResults.ElementAt(0).FundingStreamResults.First().Allocations.First().AllocationVersionNumber.Should().Be(1);
            providerResultSummary.FundingPeriodResults.ElementAt(0).FundingStreamResults.First().Allocations.First().AllocationStatus.Should().Be("Published");
            providerResultSummary.FundingPeriodResults.ElementAt(0).FundingStreamResults.First().Allocations.First().AllocationAmount.Should().Be(10);
            providerResultSummary.FundingPeriodResults.ElementAt(0).FundingStreamResults.First().Policies.Count().Should().Be(1);
            providerResultSummary.FundingPeriodResults.ElementAt(0).FundingStreamResults.First().Policies.First().Policy.PolicyId.Should().Be("239c8b47-89e6-4906-a3bf-866bd11da2f4");
            providerResultSummary.FundingPeriodResults.ElementAt(0).FundingStreamResults.First().Policies.First().Policy.PolicyName.Should().Be("Ab Test Policy 0908-001");
            providerResultSummary.FundingPeriodResults.ElementAt(0).FundingStreamResults.First().Policies.First().TotalAmount.Should().Be(300);
            providerResultSummary.FundingPeriodResults.ElementAt(0).FundingStreamResults.First().Policies.First().SubPolicyResults.Count().Should().Be(1);
            providerResultSummary.FundingPeriodResults.ElementAt(0).FundingStreamResults.First().Policies.First().Calculations.Count().Should().Be(3);
            providerResultSummary.FundingPeriodResults.ElementAt(1).FundingStreamResults.First().FundingStream.FundingStreamCode.Should().Be("fs-2");
            providerResultSummary.FundingPeriodResults.ElementAt(1).FundingStreamResults.First().FundingStream.FundingStreamName.Should().Be("fs 2");
            providerResultSummary.FundingPeriodResults.ElementAt(1).FundingStreamResults.First().TotalAmount = 100;
            providerResultSummary.FundingPeriodResults.ElementAt(1).FundingStreamResults.First().Allocations.Count().Should().Be(1);
            providerResultSummary.FundingPeriodResults.ElementAt(1).FundingStreamResults.First().Allocations.First().AllocationLine.AllocationLineCode.Should().Be("al-2");
            providerResultSummary.FundingPeriodResults.ElementAt(1).FundingStreamResults.First().Allocations.First().AllocationLine.AllocationLineName.Should().Be("al 2");
            providerResultSummary.FundingPeriodResults.ElementAt(1).FundingStreamResults.First().Allocations.First().AllocationVersionNumber.Should().Be(1);
            providerResultSummary.FundingPeriodResults.ElementAt(1).FundingStreamResults.First().Allocations.First().AllocationStatus.Should().Be("Published");
            providerResultSummary.FundingPeriodResults.ElementAt(1).FundingStreamResults.First().Allocations.First().AllocationAmount.Should().Be(100);
            providerResultSummary.FundingPeriodResults.ElementAt(1).FundingStreamResults.First().Policies.Count().Should().Be(1);
            providerResultSummary.FundingPeriodResults.ElementAt(1).FundingStreamResults.First().Policies.First().Policy.PolicyId.Should().Be("239c8b47-89e6-4906-a3bf-866bd11da2f4");
            providerResultSummary.FundingPeriodResults.ElementAt(1).FundingStreamResults.First().Policies.First().Policy.PolicyName.Should().Be("Ab Test Policy 0908-001");
            providerResultSummary.FundingPeriodResults.ElementAt(1).FundingStreamResults.First().Policies.First().TotalAmount.Should().Be(300);
            providerResultSummary.FundingPeriodResults.ElementAt(1).FundingStreamResults.First().Policies.First().SubPolicyResults.Count().Should().Be(1);
            providerResultSummary.FundingPeriodResults.ElementAt(1).FundingStreamResults.First().Policies.First().Calculations.Count().Should().Be(3);
            providerResultSummary.FundingPeriodResults.ElementAt(2).FundingStreamResults.First().FundingStream.FundingStreamCode.Should().Be("fs-3");
            providerResultSummary.FundingPeriodResults.ElementAt(2).FundingStreamResults.First().FundingStream.FundingStreamName.Should().Be("fs 3");
            providerResultSummary.FundingPeriodResults.ElementAt(2).FundingStreamResults.First().TotalAmount = 20;
            providerResultSummary.FundingPeriodResults.ElementAt(2).FundingStreamResults.First().Allocations.Count().Should().Be(1);
            providerResultSummary.FundingPeriodResults.ElementAt(2).FundingStreamResults.First().Allocations.First().AllocationLine.AllocationLineCode.Should().Be("al-3");
            providerResultSummary.FundingPeriodResults.ElementAt(2).FundingStreamResults.First().Allocations.First().AllocationLine.AllocationLineName.Should().Be("al 3");
            providerResultSummary.FundingPeriodResults.ElementAt(2).FundingStreamResults.First().Allocations.First().AllocationVersionNumber.Should().Be(1);
            providerResultSummary.FundingPeriodResults.ElementAt(2).FundingStreamResults.First().Allocations.First().AllocationStatus.Should().Be("Approved");
            providerResultSummary.FundingPeriodResults.ElementAt(2).FundingStreamResults.First().Allocations.First().AllocationAmount.Should().Be(20);
            providerResultSummary.FundingPeriodResults.ElementAt(2).FundingStreamResults.First().Policies.Count().Should().Be(1);
            providerResultSummary.FundingPeriodResults.ElementAt(2).FundingStreamResults.First().Policies.First().Policy.PolicyId.Should().Be("239c8b47-89e6-4906-a3bf-866bd11da2f4");
            providerResultSummary.FundingPeriodResults.ElementAt(2).FundingStreamResults.First().Policies.First().Policy.PolicyName.Should().Be("Ab Test Policy 0908-001");
            providerResultSummary.FundingPeriodResults.ElementAt(2).FundingStreamResults.First().Policies.First().TotalAmount.Should().Be(300);
            providerResultSummary.FundingPeriodResults.ElementAt(2).FundingStreamResults.First().Policies.First().SubPolicyResults.Count().Should().Be(1);
            providerResultSummary.FundingPeriodResults.ElementAt(2).FundingStreamResults.First().Policies.First().Calculations.Count().Should().Be(3);

            await
                searchService
                    .Received(1)
                    .GetFeeds(Arg.Is(providerId), Arg.Is(startYear), Arg.Is(endYear), Arg.Is<IList<string>>(m => m.Count == 1 && m.First() == "allocationLineId eq 'AllocationLine1'"));
        }

        [TestMethod]
        public async Task GetProviderResultsForFundingStreams_GivenMissingProviderId_ReturnsBadRequest()
        {
            //Arrange
            HttpRequest httpRequest = Substitute.For<HttpRequest>();

            ProviderResultsService providerResultsService = CreateService();

            //Act
            IActionResult result = await providerResultsService.GetProviderResultsForFundingStreams(null, startYear, endYear, allocationLineIds, httpRequest);

            //Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>()
                .Which
                .Value
                .Should()
                .Be("Missing providerId");
        }

        [TestMethod]
        public async Task GetProviderResultsForFundingStreams_GivenMissingStartYear_ReturnsBadRequest()
        {
            //Arrange
            HttpRequest httpRequest = Substitute.For<HttpRequest>();

            ProviderResultsService providerResultsService = CreateService();

            //Act
            IActionResult result = await providerResultsService.GetProviderResultsForFundingStreams(providerId, 0, endYear, fundingStreamIds, httpRequest);

            //Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>()
                .Which
                .Value
                .Should()
                .Be("Missing start year");
        }

        [TestMethod]
        public async Task GetProviderResultsForFundingStreams_GivenMissingEndYear_ReturnsBadRequest()
        {
            //Arrange
            HttpRequest httpRequest = Substitute.For<HttpRequest>();

            ProviderResultsService providerResultsService = CreateService();

            //Act
            IActionResult result = await providerResultsService.GetProviderResultsForFundingStreams(providerId, startYear, 0, fundingStreamIds, httpRequest);

            //Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>()
                .Which
                .Value
                .Should()
                .Be("Missing end year");
        }

        [TestMethod]
        public async Task GetProviderResultsForFundingStreams_GivenMissingAllocationLineIds_ReturnsBadRequest()
        {
            //Arrange
            HttpRequest httpRequest = Substitute.For<HttpRequest>();

            ProviderResultsService providerResultsService = CreateService();

            //Act
            IActionResult result = await providerResultsService.GetProviderResultsForFundingStreams(providerId, startYear, endYear, null, httpRequest);

            //Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>()
                .Which
                .Value
                .Should()
                .Be("Missing funding stream ids");
        }

        [TestMethod]
        public async Task GetProviderResultsForFundingStreams_GivenNoResultsFoundFromSearch_ReturnsNotFoundResult()
        {
            //Arrange
            HttpRequest httpRequest = Substitute.For<HttpRequest>();

            IAllocationNotificationsFeedsSearchService searchService = CreateSearchService();

            ProviderResultsService providerResultsService = CreateService(searchService);

            //Act
            IActionResult result = await providerResultsService.GetProviderResultsForFundingStreams(providerId, startYear, endYear, fundingStreamIds, httpRequest);

            //Assert
            result
                .Should()
                .BeOfType<NotFoundResult>();

            await
                searchService
                    .Received(1)
                    .GetFeeds(Arg.Is(providerId), Arg.Is(startYear), Arg.Is(endYear), Arg.Is<IList<string>>(m => m.Count == 1 && m.First() == "fundingStreamId eq 'FundingStream1'"));
        }

        [TestMethod]
        public async Task GetProviderResultsForFundingStreams_GivenSearchResultHasMissingProviderId_ReturnsNotFoundResult()
        {
            //Arrange
            HttpRequest httpRequest = Substitute.For<HttpRequest>();

            AllocationNotificationFeedIndex allocationNotificationFeedIndex = CreateFeedIndexes().First();

            allocationNotificationFeedIndex.ProviderId = null;

            SearchFeed<AllocationNotificationFeedIndex> feeds = new SearchFeed<AllocationNotificationFeedIndex>
            {
                Entries = new[] { allocationNotificationFeedIndex }
            };

            IAllocationNotificationsFeedsSearchService searchService = CreateSearchService();
            searchService
                .GetFeeds(Arg.Is(providerId), Arg.Is(startYear), Arg.Is(endYear), Arg.Any<IEnumerable<string>>())
                .Returns(feeds);

            ILogger logger = CreateLogger();

            ProviderResultsService providerResultsService = CreateService(searchService, logger);

            //Act
            IActionResult result = await providerResultsService.GetProviderResultsForFundingStreams(providerId, startYear, endYear, fundingStreamIds, httpRequest);

            //Assert
            result
                .Should()
                .BeOfType<NotFoundResult>();

            logger
                .Received(1)
                .Warning(Arg.Is($"Feed with id {allocationNotificationFeedIndex.Id} contains missing data: provider id"));
        }

        [TestMethod]
        public async Task GetProviderResultsForFundingStreams_GivenSearchResultHasMissingFundingPeriodId_ReturnsNotFoundResult()
        {
            //Arrange
            HttpRequest httpRequest = Substitute.For<HttpRequest>();

            AllocationNotificationFeedIndex allocationNotificationFeedIndex = CreateFeedIndexes().First();

            allocationNotificationFeedIndex.FundingPeriodId = null;

            SearchFeed<AllocationNotificationFeedIndex> feeds = new SearchFeed<AllocationNotificationFeedIndex>
            {
                Entries = new[] { allocationNotificationFeedIndex }
            };

            IAllocationNotificationsFeedsSearchService searchService = CreateSearchService();
            searchService
                .GetFeeds(Arg.Is(providerId), Arg.Is(startYear), Arg.Is(endYear), Arg.Any<IEnumerable<string>>())
                .Returns(feeds);

            ILogger logger = CreateLogger();

            ProviderResultsService providerResultsService = CreateService(searchService, logger);

            //Act
            IActionResult result = await providerResultsService.GetProviderResultsForFundingStreams(providerId, startYear, endYear, fundingStreamIds, httpRequest);

            //Assert
            result
                .Should()
                .BeOfType<NotFoundResult>();

            logger
                .Received(1)
                .Warning(Arg.Is($"Feed with id {allocationNotificationFeedIndex.Id} contains missing data: funding period id or type"));
        }

        [TestMethod]
        public async Task GetProviderResultsForFundingStreams_GivenSearchResultHasMissingFundingPeriodType_ReturnsNotFoundResult()
        {
            //Arrange
            HttpRequest httpRequest = Substitute.For<HttpRequest>();

            AllocationNotificationFeedIndex allocationNotificationFeedIndex = CreateFeedIndexes().First();

            allocationNotificationFeedIndex.FundingPeriodType = null;

            SearchFeed<AllocationNotificationFeedIndex> feeds = new SearchFeed<AllocationNotificationFeedIndex>
            {
                Entries = new[] { allocationNotificationFeedIndex }
            };

            IAllocationNotificationsFeedsSearchService searchService = CreateSearchService();
            searchService
                .GetFeeds(Arg.Is(providerId), Arg.Is(startYear), Arg.Is(endYear), Arg.Any<IEnumerable<string>>())
                .Returns(feeds);

            ILogger logger = CreateLogger();

            ProviderResultsService providerResultsService = CreateService(searchService, logger);

            //Act
            IActionResult result = await providerResultsService.GetProviderResultsForFundingStreams(providerId, startYear, endYear, fundingStreamIds, httpRequest);

            //Assert
            result
                .Should()
                .BeOfType<NotFoundResult>();

            logger
                .Received(1)
                .Warning(Arg.Is($"Feed with id {allocationNotificationFeedIndex.Id} contains missing data: funding period id or type"));
        }

        [TestMethod]
        public async Task GetProviderResultsForFundingStreams_GivenSearchResultHasMissingFundingStreamId_ReturnsNotFoundResult()
        {
            //Arrange
            HttpRequest httpRequest = Substitute.For<HttpRequest>();

            AllocationNotificationFeedIndex allocationNotificationFeedIndex = CreateFeedIndexes().First();

            allocationNotificationFeedIndex.FundingStreamId = null;

            SearchFeed<AllocationNotificationFeedIndex> feeds = new SearchFeed<AllocationNotificationFeedIndex>
            {
                Entries = new[] { allocationNotificationFeedIndex }
            };

            IAllocationNotificationsFeedsSearchService searchService = CreateSearchService();
            searchService
                .GetFeeds(Arg.Is(providerId), Arg.Is(startYear), Arg.Is(endYear), Arg.Any<IEnumerable<string>>())
                .Returns(feeds);

            ILogger logger = CreateLogger();

            ProviderResultsService providerResultsService = CreateService(searchService, logger);

            //Act
            IActionResult result = await providerResultsService.GetProviderResultsForFundingStreams(providerId, startYear, endYear, fundingStreamIds, httpRequest);

            //Assert
            result
                .Should()
                .BeOfType<NotFoundResult>();

            logger
                .Received(1)
                .Warning(Arg.Is($"Feed with id {allocationNotificationFeedIndex.Id} contains missing data: funding stream id or name"));
        }

        [TestMethod]
        public async Task GetProviderResultsForFundingStreams_GivenSearchResultHasMissingFundingStreamName_ReturnsNotFoundResult()
        {
            //Arrange
            HttpRequest httpRequest = Substitute.For<HttpRequest>();

            AllocationNotificationFeedIndex allocationNotificationFeedIndex = CreateFeedIndexes().First();

            allocationNotificationFeedIndex.FundingStreamName = null;

            SearchFeed<AllocationNotificationFeedIndex> feeds = new SearchFeed<AllocationNotificationFeedIndex>
            {
                Entries = new[] { allocationNotificationFeedIndex }
            };

            IAllocationNotificationsFeedsSearchService searchService = CreateSearchService();
            searchService
                .GetFeeds(Arg.Is(providerId), Arg.Is(startYear), Arg.Is(endYear), Arg.Any<IEnumerable<string>>())
                .Returns(feeds);

            ILogger logger = CreateLogger();

            ProviderResultsService providerResultsService = CreateService(searchService, logger);

            //Act
            IActionResult result = await providerResultsService.GetProviderResultsForFundingStreams(providerId, startYear, endYear, fundingStreamIds, httpRequest);

            //Assert
            result
                .Should()
                .BeOfType<NotFoundResult>();

            logger
                .Received(1)
                .Warning(Arg.Is($"Feed with id {allocationNotificationFeedIndex.Id} contains missing data: funding stream id or name"));
        }

        [TestMethod]
        public async Task GetProviderResultsForFundingStreams_GivenSearchResultHasMiddingAllocationLineId_ReturnsNotFoundResult()
        {
            //Arrange
            HttpRequest httpRequest = Substitute.For<HttpRequest>();

            AllocationNotificationFeedIndex allocationNotificationFeedIndex = CreateFeedIndexes().First();

            allocationNotificationFeedIndex.AllocationLineId = null;

            SearchFeed<AllocationNotificationFeedIndex> feeds = new SearchFeed<AllocationNotificationFeedIndex>
            {
                Entries = new[] { allocationNotificationFeedIndex }
            };

            IAllocationNotificationsFeedsSearchService searchService = CreateSearchService();
            searchService
                .GetFeeds(Arg.Is(providerId), Arg.Is(startYear), Arg.Is(endYear), Arg.Any<IEnumerable<string>>())
                .Returns(feeds);

            ILogger logger = CreateLogger();

            ProviderResultsService providerResultsService = CreateService(searchService, logger);

            //Act
            IActionResult result = await providerResultsService.GetProviderResultsForFundingStreams(providerId, startYear, endYear, fundingStreamIds, httpRequest);

            //Assert
            result
                .Should()
                .BeOfType<NotFoundResult>();

            logger
                .Received(1)
                .Warning(Arg.Is($"Feed with id {allocationNotificationFeedIndex.Id} contains missing data: allocation line id or name"));
        }

        [TestMethod]
        public async Task GetProviderResultsForFundingStreams_GivenSearchResultHasMissingAllocationLineName_ReturnsNotFoundResult()
        {
            //Arrange
            HttpRequest httpRequest = Substitute.For<HttpRequest>();

            AllocationNotificationFeedIndex allocationNotificationFeedIndex = CreateFeedIndexes().First();

            allocationNotificationFeedIndex.AllocationLineName = null;

            SearchFeed<AllocationNotificationFeedIndex> feeds = new SearchFeed<AllocationNotificationFeedIndex>
            {
                Entries = new[] { allocationNotificationFeedIndex }
            };

            IAllocationNotificationsFeedsSearchService searchService = CreateSearchService();
            searchService
                .GetFeeds(Arg.Is(providerId), Arg.Is(startYear), Arg.Is(endYear), Arg.Any<IEnumerable<string>>())
                .Returns(feeds);

            ILogger logger = CreateLogger();

            ProviderResultsService providerResultsService = CreateService(searchService, logger);

            //Act
            IActionResult result = await providerResultsService.GetProviderResultsForFundingStreams(providerId, startYear, endYear, fundingStreamIds, httpRequest);

            //Assert
            result
                .Should()
                .BeOfType<NotFoundResult>();

            logger
                .Received(1)
                .Warning(Arg.Is($"Feed with id {allocationNotificationFeedIndex.Id} contains missing data: allocation line id or name"));
        }

        [TestMethod]
        public async Task GetProviderResultsForFundingStreams_GivenSearchResultHasEmptyProfiles_ReturnsNotFoundResult()
        {
            //Arrange
            HttpRequest httpRequest = Substitute.For<HttpRequest>();

            AllocationNotificationFeedIndex allocationNotificationFeedIndex = CreateFeedIndexes().First();

            allocationNotificationFeedIndex.PolicySummaries = "[]";

            SearchFeed<AllocationNotificationFeedIndex> feeds = new SearchFeed<AllocationNotificationFeedIndex>
            {
                Entries = new[] { allocationNotificationFeedIndex }
            };

            IAllocationNotificationsFeedsSearchService searchService = CreateSearchService();
            searchService
                .GetFeeds(Arg.Is(providerId), Arg.Is(startYear), Arg.Is(endYear), Arg.Any<IEnumerable<string>>())
                .Returns(feeds);

            ILogger logger = CreateLogger();

            ProviderResultsService providerResultsService = CreateService(searchService, logger);

            //Act
            IActionResult result = await providerResultsService.GetProviderResultsForFundingStreams(providerId, startYear, endYear, fundingStreamIds, httpRequest);

            //Assert
            result
                .Should()
                .BeOfType<NotFoundResult>();

            logger
                .Received(1)
                .Warning(Arg.Is($"Feed with id {allocationNotificationFeedIndex.Id} contains missing data: policy summaries"));
        }

        [TestMethod]
        public async Task GetProviderResultsForFundingStreams_GivenSearchResultHasInvalidProfiless_ReturnsNotFoundResult()
        {
            //Arrange
            HttpRequest httpRequest = Substitute.For<HttpRequest>();

            AllocationNotificationFeedIndex allocationNotificationFeedIndex = CreateFeedIndexes().First();

            allocationNotificationFeedIndex.PolicySummaries = "whatever";

            SearchFeed<AllocationNotificationFeedIndex> feeds = new SearchFeed<AllocationNotificationFeedIndex>
            {
                Entries = new[] { allocationNotificationFeedIndex }
            };

            IAllocationNotificationsFeedsSearchService searchService = CreateSearchService();
            searchService
                .GetFeeds(Arg.Is(providerId), Arg.Is(startYear), Arg.Is(endYear), Arg.Any<IEnumerable<string>>())
                .Returns(feeds);

            ILogger logger = CreateLogger();

            ProviderResultsService providerResultsService = CreateService(searchService, logger);

            //Act
            IActionResult result = await providerResultsService.GetProviderResultsForFundingStreams(providerId, startYear, endYear, fundingStreamIds, httpRequest);

            //Assert
            result
                .Should()
                .BeOfType<NotFoundResult>();

            logger
                .Received(1)
                .Error(Arg.Any<Exception>(), Arg.Is($"Failed to deserialize policies for feed id {allocationNotificationFeedIndex.Id}"));
        }

        [TestMethod]
        public async Task GetProviderResultsForFundingStreams_GivenValidResultsFoundFromSearch_ReturnsResults()
        {
            //Arrange
            IHeaderDictionary headerDictionary = new HeaderDictionary();
            headerDictionary.Add("Accept", new StringValues("application/json"));

            HttpRequest httpRequest = Substitute.For<HttpRequest>();
            httpRequest.Headers.Returns(headerDictionary);

            SearchFeed<AllocationNotificationFeedIndex> feeds = new SearchFeed<AllocationNotificationFeedIndex>
            {
                Entries = CreateFeedIndexes()
            };

            IAllocationNotificationsFeedsSearchService searchService = CreateSearchService();
            searchService
                .GetFeeds(Arg.Is(providerId), Arg.Is(startYear), Arg.Is(endYear), Arg.Any<IEnumerable<string>>())
                .Returns(feeds);

            ProviderResultsService providerResultsService = CreateService(searchService);

            //Act
            IActionResult result = await providerResultsService.GetProviderResultsForFundingStreams(providerId, startYear, endYear, fundingStreamIds, httpRequest);

            //Assert
            result
                .Should()
                .BeOfType<ContentResult>()
                .Which
                .StatusCode
                .Should()
                .Be(200);

            ContentResult contentResult = result as ContentResult;

            ProviderResultSummary providerResultSummary = JsonConvert.DeserializeObject<ProviderResultSummary>(contentResult.Content);

            providerResultSummary.TotalAmount.Should().Be(130);
            providerResultSummary.Provider.Ukprn.Should().Be("1111");
            providerResultSummary.FundingPeriodResults.Count().Should().Be(3);
            providerResultSummary.FundingPeriodResults.ElementAt(0).FundingStreamResults.Count().Should().Be(1);
            providerResultSummary.FundingPeriodResults.ElementAt(1).FundingStreamResults.Count().Should().Be(1);
            providerResultSummary.FundingPeriodResults.ElementAt(2).FundingStreamResults.Count().Should().Be(1);
            providerResultSummary.FundingPeriodResults.ElementAt(0).FundingStreamResults.First().FundingStream.FundingStreamCode.Should().Be("fs-1");
            providerResultSummary.FundingPeriodResults.ElementAt(0).FundingStreamResults.First().FundingStream.FundingStreamName.Should().Be("fs 1");
            providerResultSummary.FundingPeriodResults.ElementAt(0).FundingStreamResults.First().TotalAmount = 10;
            providerResultSummary.FundingPeriodResults.ElementAt(0).FundingStreamResults.First().Allocations.Count().Should().Be(1);
            providerResultSummary.FundingPeriodResults.ElementAt(0).FundingStreamResults.First().Allocations.First().AllocationLine.AllocationLineCode.Should().Be("al-1");
            providerResultSummary.FundingPeriodResults.ElementAt(0).FundingStreamResults.First().Allocations.First().AllocationLine.AllocationLineName.Should().Be("al 1");
            providerResultSummary.FundingPeriodResults.ElementAt(0).FundingStreamResults.First().Allocations.First().AllocationVersionNumber.Should().Be(1);
            providerResultSummary.FundingPeriodResults.ElementAt(0).FundingStreamResults.First().Allocations.First().AllocationStatus.Should().Be("Published");
            providerResultSummary.FundingPeriodResults.ElementAt(0).FundingStreamResults.First().Allocations.First().AllocationAmount.Should().Be(10);
            providerResultSummary.FundingPeriodResults.ElementAt(0).FundingStreamResults.First().Policies.Count().Should().Be(1);
            providerResultSummary.FundingPeriodResults.ElementAt(0).FundingStreamResults.First().Policies.First().Policy.PolicyId.Should().Be("239c8b47-89e6-4906-a3bf-866bd11da2f4");
            providerResultSummary.FundingPeriodResults.ElementAt(0).FundingStreamResults.First().Policies.First().Policy.PolicyName.Should().Be("Ab Test Policy 0908-001");
            providerResultSummary.FundingPeriodResults.ElementAt(0).FundingStreamResults.First().Policies.First().TotalAmount.Should().Be(300);
            providerResultSummary.FundingPeriodResults.ElementAt(0).FundingStreamResults.First().Policies.First().SubPolicyResults.Count().Should().Be(1);
            providerResultSummary.FundingPeriodResults.ElementAt(0).FundingStreamResults.First().Policies.First().Calculations.Count().Should().Be(3);
            providerResultSummary.FundingPeriodResults.ElementAt(1).FundingStreamResults.First().FundingStream.FundingStreamCode.Should().Be("fs-2");
            providerResultSummary.FundingPeriodResults.ElementAt(1).FundingStreamResults.First().FundingStream.FundingStreamName.Should().Be("fs 2");
            providerResultSummary.FundingPeriodResults.ElementAt(1).FundingStreamResults.First().TotalAmount = 100;
            providerResultSummary.FundingPeriodResults.ElementAt(1).FundingStreamResults.First().Allocations.Count().Should().Be(1);
            providerResultSummary.FundingPeriodResults.ElementAt(1).FundingStreamResults.First().Allocations.First().AllocationLine.AllocationLineCode.Should().Be("al-2");
            providerResultSummary.FundingPeriodResults.ElementAt(1).FundingStreamResults.First().Allocations.First().AllocationLine.AllocationLineName.Should().Be("al 2");
            providerResultSummary.FundingPeriodResults.ElementAt(1).FundingStreamResults.First().Allocations.First().AllocationVersionNumber.Should().Be(1);
            providerResultSummary.FundingPeriodResults.ElementAt(1).FundingStreamResults.First().Allocations.First().AllocationStatus.Should().Be("Published");
            providerResultSummary.FundingPeriodResults.ElementAt(1).FundingStreamResults.First().Allocations.First().AllocationAmount.Should().Be(100);
            providerResultSummary.FundingPeriodResults.ElementAt(1).FundingStreamResults.First().Policies.Count().Should().Be(1);
            providerResultSummary.FundingPeriodResults.ElementAt(1).FundingStreamResults.First().Policies.First().Policy.PolicyId.Should().Be("239c8b47-89e6-4906-a3bf-866bd11da2f4");
            providerResultSummary.FundingPeriodResults.ElementAt(1).FundingStreamResults.First().Policies.First().Policy.PolicyName.Should().Be("Ab Test Policy 0908-001");
            providerResultSummary.FundingPeriodResults.ElementAt(1).FundingStreamResults.First().Policies.First().TotalAmount.Should().Be(300);
            providerResultSummary.FundingPeriodResults.ElementAt(1).FundingStreamResults.First().Policies.First().SubPolicyResults.Count().Should().Be(1);
            providerResultSummary.FundingPeriodResults.ElementAt(1).FundingStreamResults.First().Policies.First().Calculations.Count().Should().Be(3);
            providerResultSummary.FundingPeriodResults.ElementAt(2).FundingStreamResults.First().FundingStream.FundingStreamCode.Should().Be("fs-3");
            providerResultSummary.FundingPeriodResults.ElementAt(2).FundingStreamResults.First().FundingStream.FundingStreamName.Should().Be("fs 3");
            providerResultSummary.FundingPeriodResults.ElementAt(2).FundingStreamResults.First().TotalAmount = 20;
            providerResultSummary.FundingPeriodResults.ElementAt(2).FundingStreamResults.First().Allocations.Count().Should().Be(1);
            providerResultSummary.FundingPeriodResults.ElementAt(2).FundingStreamResults.First().Allocations.First().AllocationLine.AllocationLineCode.Should().Be("al-3");
            providerResultSummary.FundingPeriodResults.ElementAt(2).FundingStreamResults.First().Allocations.First().AllocationLine.AllocationLineName.Should().Be("al 3");
            providerResultSummary.FundingPeriodResults.ElementAt(2).FundingStreamResults.First().Allocations.First().AllocationVersionNumber.Should().Be(1);
            providerResultSummary.FundingPeriodResults.ElementAt(2).FundingStreamResults.First().Allocations.First().AllocationStatus.Should().Be("Approved");
            providerResultSummary.FundingPeriodResults.ElementAt(2).FundingStreamResults.First().Allocations.First().AllocationAmount.Should().Be(20);
            providerResultSummary.FundingPeriodResults.ElementAt(2).FundingStreamResults.First().Policies.Count().Should().Be(1);
            providerResultSummary.FundingPeriodResults.ElementAt(2).FundingStreamResults.First().Policies.First().Policy.PolicyId.Should().Be("239c8b47-89e6-4906-a3bf-866bd11da2f4");
            providerResultSummary.FundingPeriodResults.ElementAt(2).FundingStreamResults.First().Policies.First().Policy.PolicyName.Should().Be("Ab Test Policy 0908-001");
            providerResultSummary.FundingPeriodResults.ElementAt(2).FundingStreamResults.First().Policies.First().TotalAmount.Should().Be(300);
            providerResultSummary.FundingPeriodResults.ElementAt(2).FundingStreamResults.First().Policies.First().SubPolicyResults.Count().Should().Be(1);
            providerResultSummary.FundingPeriodResults.ElementAt(2).FundingStreamResults.First().Policies.First().Calculations.Count().Should().Be(3);

            await
                searchService
                    .Received(1)
                    .GetFeeds(Arg.Is(providerId), Arg.Is(startYear), Arg.Is(endYear), Arg.Is<IList<string>>(m => m.Count == 1 && m.First() == "fundingStreamId eq 'FundingStream1'"));
        }

        [TestMethod]
        public async Task GetLocalAuthorityProvidersResultsForAllocations_GivenMissingLaCode_ReturnsBadRequest()
        {
            //Arrange
            HttpRequest httpRequest = Substitute.For<HttpRequest>();

            ProviderResultsService providerResultsService = CreateService();

            //Act
            IActionResult result = await providerResultsService.GetLocalAuthorityProvidersResultsForAllocations(null, startYear, endYear, allocationLineIds, httpRequest);

            //Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>()
                .Which
                .Value
                .Should()
                .Be("Missing la code");
        }

        [TestMethod]
        public async Task GetLocalAuthorityProvidersResultsForAllocations_GivenMissingStartYear_ReturnsBadRequest()
        {
            //Arrange
            HttpRequest httpRequest = Substitute.For<HttpRequest>();

            ProviderResultsService providerResultsService = CreateService();

            //Act
            IActionResult result = await providerResultsService.GetLocalAuthorityProvidersResultsForAllocations(laCode, 0, endYear, allocationLineIds, httpRequest);

            //Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>()
                .Which
                .Value
                .Should()
                .Be("Missing start year");
        }

        [TestMethod]
        public async Task GetLocalAuthorityProvidersResultsForAllocations_GivenMissingEndYear_ReturnsBadRequest()
        {
            //Arrange
            HttpRequest httpRequest = Substitute.For<HttpRequest>();

            ProviderResultsService providerResultsService = CreateService();

            //Act
            IActionResult result = await providerResultsService.GetLocalAuthorityProvidersResultsForAllocations(laCode, startYear, 0, allocationLineIds, httpRequest);

            //Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>()
                .Which
                .Value
                .Should()
                .Be("Missing end year");
        }

        [TestMethod]
        public async Task GetLocalAuthorityProvidersResultsForAllocations_GivenMissingAllocationLineIds_ReturnsBadRequest()
        {
            //Arrange
            HttpRequest httpRequest = Substitute.For<HttpRequest>();

            ProviderResultsService providerResultsService = CreateService();

            //Act
            IActionResult result = await providerResultsService.GetLocalAuthorityProvidersResultsForAllocations(laCode, startYear, endYear, null, httpRequest);

            //Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>()
                .Which
                .Value
                .Should()
                .Be("Missing allocation line ids");
        }

        [TestMethod]
        public async Task GetLocalAuthorityProvidersResultsForAllocations_GivenNoResultsFoundFromSearch_ReturnsNotFoundResult()
        {
            //Arrange
            HttpRequest httpRequest = Substitute.For<HttpRequest>();

            IAllocationNotificationsFeedsSearchService searchService = CreateSearchService();

            ProviderResultsService providerResultsService = CreateService(searchService);

            //Act
            IActionResult result = await providerResultsService.GetLocalAuthorityProvidersResultsForAllocations(laCode, startYear, endYear, allocationLineIds, httpRequest);

            //Assert
            result
                .Should()
                .BeOfType<NotFoundResult>();

            await
                searchService
                    .Received(1)
                    .GetLocalAuthorityFeeds(Arg.Is(laCode), Arg.Is(startYear), Arg.Is(endYear), Arg.Is<IList<string>>(m => m.Count == 1 && m.First() == "allocationLineId eq 'AllocationLine1'"));
        }

        [TestMethod]
        public async Task GetLocalAuthorityProvidersResultsForAllocations_GivenSearchResultHasMissingProviderId_ReturnsNotFoundResult()
        {
            //Arrange
            HttpRequest httpRequest = Substitute.For<HttpRequest>();

            AllocationNotificationFeedIndex allocationNotificationFeedIndex = CreateFeedIndexes().First();

            allocationNotificationFeedIndex.ProviderId = null;

            SearchFeed<AllocationNotificationFeedIndex> feeds = new SearchFeed<AllocationNotificationFeedIndex>
            {
                Entries = new[] { allocationNotificationFeedIndex }
            };

            IAllocationNotificationsFeedsSearchService searchService = CreateSearchService();
            searchService
                .GetLocalAuthorityFeeds(Arg.Is(laCode), Arg.Is(startYear), Arg.Is(endYear), Arg.Any<IEnumerable<string>>())
                .Returns(feeds);

            ILogger logger = CreateLogger();

            ProviderResultsService providerResultsService = CreateService(searchService, logger);

            //Act
            IActionResult result = await providerResultsService.GetLocalAuthorityProvidersResultsForAllocations(laCode, startYear, endYear, fundingStreamIds, httpRequest);

            //Assert
            result
                .Should()
                .BeOfType<NotFoundResult>();

            logger
                .Received(1)
                .Warning(Arg.Is($"Feed with id {allocationNotificationFeedIndex.Id} contains missing data: provider id"));
        }

        [TestMethod]
        public async Task GetLocalAuthorityProvidersResultsForAllocations_GivenSearchResultHasMissingLACode_ReturnsNotFoundResult()
        {
            //Arrange
            HttpRequest httpRequest = Substitute.For<HttpRequest>();

            AllocationNotificationFeedIndex allocationNotificationFeedIndex = CreateFeedIndexes().First();

            allocationNotificationFeedIndex.LaCode = null;

            SearchFeed<AllocationNotificationFeedIndex> feeds = new SearchFeed<AllocationNotificationFeedIndex>
            {
                Entries = new[] { allocationNotificationFeedIndex }
            };

            IAllocationNotificationsFeedsSearchService searchService = CreateSearchService();
            searchService
                .GetLocalAuthorityFeeds(Arg.Is(laCode), Arg.Is(startYear), Arg.Is(endYear), Arg.Any<IEnumerable<string>>())
                .Returns(feeds);

            ILogger logger = CreateLogger();

            ProviderResultsService providerResultsService = CreateService(searchService, logger);

            //Act
            IActionResult result = await providerResultsService.GetLocalAuthorityProvidersResultsForAllocations(laCode, startYear, endYear, fundingStreamIds, httpRequest);

            //Assert
            result
                .Should()
                .BeOfType<NotFoundResult>();

            logger
                .Received(1)
                .Warning(Arg.Is($"Feed with id {allocationNotificationFeedIndex.Id} contains missing data: la code"));
        }

        [TestMethod]
        public async Task GetLocalAuthorityProvidersResultsForAllocations_GivenSearchResultHasMissingFundingPeriodId_ReturnsNotFoundResult()
        {
            //Arrange
            HttpRequest httpRequest = Substitute.For<HttpRequest>();

            AllocationNotificationFeedIndex allocationNotificationFeedIndex = CreateFeedIndexes().First();

            allocationNotificationFeedIndex.FundingPeriodId = null;

            SearchFeed<AllocationNotificationFeedIndex> feeds = new SearchFeed<AllocationNotificationFeedIndex>
            {
                Entries = new[] { allocationNotificationFeedIndex }
            };

            IAllocationNotificationsFeedsSearchService searchService = CreateSearchService();
            searchService
                .GetLocalAuthorityFeeds(Arg.Is(laCode), Arg.Is(startYear), Arg.Is(endYear), Arg.Any<IEnumerable<string>>())
                .Returns(feeds);

            ILogger logger = CreateLogger();

            ProviderResultsService providerResultsService = CreateService(searchService, logger);

            //Act
            IActionResult result = await providerResultsService.GetLocalAuthorityProvidersResultsForAllocations(laCode, startYear, endYear, fundingStreamIds, httpRequest);

            //Assert
            result
                .Should()
                .BeOfType<NotFoundResult>();

            logger
                .Received(1)
                .Warning(Arg.Is($"Feed with id {allocationNotificationFeedIndex.Id} contains missing data: funding period id or type"));
        }

        [TestMethod]
        public async Task GetLocalAuthorityProvidersResultsForAllocations_GivenSearchResultHasMissingFundingPeriodType_ReturnsNotFoundResult()
        {
            //Arrange
            HttpRequest httpRequest = Substitute.For<HttpRequest>();

            AllocationNotificationFeedIndex allocationNotificationFeedIndex = CreateFeedIndexes().First();

            allocationNotificationFeedIndex.FundingPeriodType = null;

            SearchFeed<AllocationNotificationFeedIndex> feeds = new SearchFeed<AllocationNotificationFeedIndex>
            {
                Entries = new[] { allocationNotificationFeedIndex }
            };

            IAllocationNotificationsFeedsSearchService searchService = CreateSearchService();
            searchService
                .GetLocalAuthorityFeeds(Arg.Is(laCode), Arg.Is(startYear), Arg.Is(endYear), Arg.Any<IEnumerable<string>>())
                .Returns(feeds);

            ILogger logger = CreateLogger();

            ProviderResultsService providerResultsService = CreateService(searchService, logger);

            //Act
            IActionResult result = await providerResultsService.GetLocalAuthorityProvidersResultsForAllocations(laCode, startYear, endYear, fundingStreamIds, httpRequest);

            //Assert
            result
                .Should()
                .BeOfType<NotFoundResult>();

            logger
                .Received(1)
                .Warning(Arg.Is($"Feed with id {allocationNotificationFeedIndex.Id} contains missing data: funding period id or type"));
        }

        [TestMethod]
        public async Task GetLocalAuthorityProvidersResultsForAllocations_GivenSearchResultHasMissingFundingStreamId_ReturnsNotFoundResult()
        {
            //Arrange
            HttpRequest httpRequest = Substitute.For<HttpRequest>();

            AllocationNotificationFeedIndex allocationNotificationFeedIndex = CreateFeedIndexes().First();

            allocationNotificationFeedIndex.FundingStreamId = null;

            SearchFeed<AllocationNotificationFeedIndex> feeds = new SearchFeed<AllocationNotificationFeedIndex>
            {
                Entries = new[] { allocationNotificationFeedIndex }
            };

            IAllocationNotificationsFeedsSearchService searchService = CreateSearchService();
            searchService
                .GetLocalAuthorityFeeds(Arg.Is(laCode), Arg.Is(startYear), Arg.Is(endYear), Arg.Any<IEnumerable<string>>())
                .Returns(feeds);

            ILogger logger = CreateLogger();

            ProviderResultsService providerResultsService = CreateService(searchService, logger);

            //Act
            IActionResult result = await providerResultsService.GetLocalAuthorityProvidersResultsForAllocations(laCode, startYear, endYear, fundingStreamIds, httpRequest);

            //Assert
            result
                .Should()
                .BeOfType<NotFoundResult>();

            logger
                .Received(1)
                .Warning(Arg.Is($"Feed with id {allocationNotificationFeedIndex.Id} contains missing data: funding stream id or name"));
        }

        [TestMethod]
        public async Task GetLocalAuthorityProvidersResultsForAllocations_GivenSearchResultHasMissingFundingStreamName_ReturnsNotFoundResult()
        {
            //Arrange
            HttpRequest httpRequest = Substitute.For<HttpRequest>();

            AllocationNotificationFeedIndex allocationNotificationFeedIndex = CreateFeedIndexes().First();

            allocationNotificationFeedIndex.FundingStreamName = null;

            SearchFeed<AllocationNotificationFeedIndex> feeds = new SearchFeed<AllocationNotificationFeedIndex>
            {
                Entries = new[] { allocationNotificationFeedIndex }
            };

            IAllocationNotificationsFeedsSearchService searchService = CreateSearchService();
            searchService
                .GetLocalAuthorityFeeds(Arg.Is(laCode), Arg.Is(startYear), Arg.Is(endYear), Arg.Any<IEnumerable<string>>())
                .Returns(feeds);

            ILogger logger = CreateLogger();

            ProviderResultsService providerResultsService = CreateService(searchService, logger);

            //Act
            IActionResult result = await providerResultsService.GetLocalAuthorityProvidersResultsForAllocations(laCode, startYear, endYear, fundingStreamIds, httpRequest);

            //Assert
            result
                .Should()
                .BeOfType<NotFoundResult>();

            logger
                .Received(1)
                .Warning(Arg.Is($"Feed with id {allocationNotificationFeedIndex.Id} contains missing data: funding stream id or name"));
        }

        [TestMethod]
        public async Task GetLocalAuthorityProvidersResultsForAllocations_GivenSearchResultHasMissingAllocationLineId_ReturnsNotFoundResult()
        {
            //Arrange
            HttpRequest httpRequest = Substitute.For<HttpRequest>();

            AllocationNotificationFeedIndex allocationNotificationFeedIndex = CreateFeedIndexes().First();

            allocationNotificationFeedIndex.AllocationLineId = null;

            SearchFeed<AllocationNotificationFeedIndex> feeds = new SearchFeed<AllocationNotificationFeedIndex>
            {
                Entries = new[] { allocationNotificationFeedIndex }
            };

            IAllocationNotificationsFeedsSearchService searchService = CreateSearchService();
            searchService
                .GetLocalAuthorityFeeds(Arg.Is(laCode), Arg.Is(startYear), Arg.Is(endYear), Arg.Any<IEnumerable<string>>())
                .Returns(feeds);

            ILogger logger = CreateLogger();

            ProviderResultsService providerResultsService = CreateService(searchService, logger);

            //Act
            IActionResult result = await providerResultsService.GetLocalAuthorityProvidersResultsForAllocations(laCode, startYear, endYear, fundingStreamIds, httpRequest);

            //Assert
            result
                .Should()
                .BeOfType<NotFoundResult>();

            logger
                .Received(1)
                .Warning(Arg.Is($"Feed with id {allocationNotificationFeedIndex.Id} contains missing data: allocation line id or name"));
        }

        [TestMethod]
        public async Task GetLocalAuthorityProvidersResultsForAllocations_GivenSearchResultHasMissingAllocationLineName_ReturnsNotFoundResult()
        {
            //Arrange
            HttpRequest httpRequest = Substitute.For<HttpRequest>();

            AllocationNotificationFeedIndex allocationNotificationFeedIndex = CreateFeedIndexes().First();

            allocationNotificationFeedIndex.AllocationLineName = null;

            SearchFeed<AllocationNotificationFeedIndex> feeds = new SearchFeed<AllocationNotificationFeedIndex>
            {
                Entries = new[] { allocationNotificationFeedIndex }
            };

            IAllocationNotificationsFeedsSearchService searchService = CreateSearchService();
            searchService
                .GetLocalAuthorityFeeds(Arg.Is(laCode), Arg.Is(startYear), Arg.Is(endYear), Arg.Any<IEnumerable<string>>())
                .Returns(feeds);

            ILogger logger = CreateLogger();

            ProviderResultsService providerResultsService = CreateService(searchService, logger);

            //Act
            IActionResult result = await providerResultsService.GetLocalAuthorityProvidersResultsForAllocations(laCode, startYear, endYear, fundingStreamIds, httpRequest);

            //Assert
            result
                .Should()
                .BeOfType<NotFoundResult>();

            logger
                .Received(1)
                .Warning(Arg.Is($"Feed with id {allocationNotificationFeedIndex.Id} contains missing data: allocation line id or name"));
        }

        [TestMethod]
        public async Task GetLocalAuthorityProvidersResultsForAllocations_GivenSearchResultHasEmptyProfiles_ReturnsNotFoundResult()
        {
            //Arrange
            HttpRequest httpRequest = Substitute.For<HttpRequest>();

            AllocationNotificationFeedIndex allocationNotificationFeedIndex = CreateFeedIndexes().First();

            allocationNotificationFeedIndex.ProviderProfiling = "[]";

            SearchFeed<AllocationNotificationFeedIndex> feeds = new SearchFeed<AllocationNotificationFeedIndex>
            {
                Entries = new[] { allocationNotificationFeedIndex }
            };

            IAllocationNotificationsFeedsSearchService searchService = CreateSearchService();
            searchService
                .GetLocalAuthorityFeeds(Arg.Is(laCode), Arg.Is(startYear), Arg.Is(endYear), Arg.Any<IEnumerable<string>>())
                .Returns(feeds);

            ILogger logger = CreateLogger();

            ProviderResultsService providerResultsService = CreateService(searchService, logger);

            //Act
            IActionResult result = await providerResultsService.GetLocalAuthorityProvidersResultsForAllocations(laCode, startYear, endYear, allocationLineIds, httpRequest);

            //Assert
            result
                .Should()
                .BeOfType<NotFoundResult>();

            logger
                .Received(1)
                .Warning(Arg.Is($"Feed with id {allocationNotificationFeedIndex.Id} contains missing data: provider profiles"));
        }

        [TestMethod]
        public async Task GetLocalAuthorityProvidersResultsForAllocations_GivenSearchResultHasInvalidProfiles_ReturnsNotFoundResult()
        {
            //Arrange
            HttpRequest httpRequest = Substitute.For<HttpRequest>();

            AllocationNotificationFeedIndex allocationNotificationFeedIndex = CreateFeedIndexes().First();

            allocationNotificationFeedIndex.ProviderProfiling = "whatever";

            SearchFeed<AllocationNotificationFeedIndex> feeds = new SearchFeed<AllocationNotificationFeedIndex>
            {
                Entries = new[] { allocationNotificationFeedIndex }
            };

            IAllocationNotificationsFeedsSearchService searchService = CreateSearchService();
            searchService
                .GetLocalAuthorityFeeds(Arg.Is(laCode), Arg.Is(startYear), Arg.Is(endYear), Arg.Any<IEnumerable<string>>())
                .Returns(feeds);

            ILogger logger = CreateLogger();

            ProviderResultsService providerResultsService = CreateService(searchService, logger);

            //Act
            IActionResult result = await providerResultsService.GetLocalAuthorityProvidersResultsForAllocations(laCode, startYear, endYear, allocationLineIds, httpRequest);

            //Assert
            result
                .Should()
                .BeOfType<NotFoundResult>();

            logger
                .Received(1)
                .Error(Arg.Any<Exception>(), Arg.Is($"Failed to deserialize provider profiles for feed id {allocationNotificationFeedIndex.Id}"));
        }

        [TestMethod]
        public async Task GetLocalAuthorityProvidersResultsForAllocations_GivenValidResultsFoundFromSearch_ReturnsResults()
        {
            //Arrange
            IHeaderDictionary headerDictionary = new HeaderDictionary();
            headerDictionary.Add("Accept", new StringValues("application/json"));

            HttpRequest httpRequest = Substitute.For<HttpRequest>();
            httpRequest.Headers.Returns(headerDictionary);

            SearchFeed<AllocationNotificationFeedIndex> feeds = new SearchFeed<AllocationNotificationFeedIndex>
            {
                Entries = CreateFeedIndexes()
            };

            IAllocationNotificationsFeedsSearchService searchService = CreateSearchService();
            searchService
                .GetLocalAuthorityFeeds(Arg.Is(laCode), Arg.Is(startYear), Arg.Is(endYear), Arg.Any<IEnumerable<string>>())
                .Returns(feeds);

            ProviderResultsService providerResultsService = CreateService(searchService);

            //Act
            IActionResult result = await providerResultsService.GetLocalAuthorityProvidersResultsForAllocations(laCode, startYear, endYear, allocationLineIds, httpRequest);

            //Assert
            result
                .Should()
                .BeOfType<ContentResult>()
                .Which
                .StatusCode
                .Should()
                .Be(200);

            ContentResult contentResult = result as ContentResult;

            LocalAuthorityResultsSummary localAuthorityResultSummary = JsonConvert.DeserializeObject<LocalAuthorityResultsSummary>(contentResult.Content);

            localAuthorityResultSummary.FundingPeriod.Should().Be($"{DateTimeOffset.Now.Year}-{DateTimeOffset.Now.AddYears(1).Year}");
            localAuthorityResultSummary.LocalAuthorities.Count().Should().Be(1);
            localAuthorityResultSummary.LocalAuthorities.First().LANo.Should().Be("123");
            localAuthorityResultSummary.LocalAuthorities.First().LAName.Should().Be("LA 1");
            localAuthorityResultSummary.LocalAuthorities.First().Providers.Count().Should().Be(3);
            localAuthorityResultSummary.LocalAuthorities.First().Providers.ElementAt(0).Ukprn.Should().Be("1111");
            localAuthorityResultSummary.LocalAuthorities.First().Providers.ElementAt(0).OrganisationName.Should().Be("Provider 1");
            localAuthorityResultSummary.LocalAuthorities.First().Providers.ElementAt(0).OrganisationType.Should().Be("Provider Type 1");
            localAuthorityResultSummary.LocalAuthorities.First().Providers.ElementAt(0).OrganisationSubType.Should().Be("Sub Provider Type 1");
            localAuthorityResultSummary.LocalAuthorities.First().Providers.ElementAt(0).AllocationValue.Should().Be(10);
            localAuthorityResultSummary.LocalAuthorities.First().Providers.ElementAt(0).FundingPeriods.Count().Should().Be(3);
            localAuthorityResultSummary.LocalAuthorities.First().Providers.ElementAt(0).FundingPeriods.First().FundingPeriod.PeriodId.Should().Be("fp-1");
            localAuthorityResultSummary.LocalAuthorities.First().Providers.ElementAt(0).FundingPeriods.First().FundingPeriod.PeriodType.Should().Be("fp type 1");
            localAuthorityResultSummary.LocalAuthorities.First().Providers.ElementAt(0).FundingPeriods.First().Allocations.Count().Should().Be(1);
            localAuthorityResultSummary.LocalAuthorities.First().Providers.ElementAt(0).FundingPeriods.First().Allocations.First().AllocationLine.AllocationLineCode.Should().Be("al-1");
            localAuthorityResultSummary.LocalAuthorities.First().Providers.ElementAt(0).FundingPeriods.First().Allocations.First().AllocationLine.AllocationLineName.Should().Be("al 1");
            localAuthorityResultSummary.LocalAuthorities.First().Providers.ElementAt(0).FundingPeriods.First().Allocations.First().AllocationVersionNumber.Should().Be(1);
            localAuthorityResultSummary.LocalAuthorities.First().Providers.ElementAt(0).FundingPeriods.First().Allocations.First().AllocationStatus.Should().Be("Published");
            localAuthorityResultSummary.LocalAuthorities.First().Providers.ElementAt(0).FundingPeriods.First().Allocations.First().AllocationAmount.Should().Be(10);
            localAuthorityResultSummary.LocalAuthorities.First().Providers.ElementAt(1).Ukprn.Should().Be("2222");
            localAuthorityResultSummary.LocalAuthorities.First().Providers.ElementAt(1).OrganisationName.Should().Be("Provider 2");
            localAuthorityResultSummary.LocalAuthorities.First().Providers.ElementAt(1).OrganisationType.Should().Be("Provider Type 1");
            localAuthorityResultSummary.LocalAuthorities.First().Providers.ElementAt(1).OrganisationSubType.Should().Be("Sub Provider Type 1");
            localAuthorityResultSummary.LocalAuthorities.First().Providers.ElementAt(1).AllocationValue.Should().Be(100);
            localAuthorityResultSummary.LocalAuthorities.First().Providers.ElementAt(1).FundingPeriods.Count().Should().Be(3);
            localAuthorityResultSummary.LocalAuthorities.First().Providers.ElementAt(1).FundingPeriods.ElementAt(1).FundingPeriod.PeriodId.Should().Be("fp-2");
            localAuthorityResultSummary.LocalAuthorities.First().Providers.ElementAt(1).FundingPeriods.ElementAt(1).FundingPeriod.PeriodType.Should().Be("fp type 2");
            localAuthorityResultSummary.LocalAuthorities.First().Providers.ElementAt(1).FundingPeriods.ElementAt(1).Allocations.Count().Should().Be(1);
            localAuthorityResultSummary.LocalAuthorities.First().Providers.ElementAt(1).FundingPeriods.ElementAt(1).Allocations.First().AllocationLine.AllocationLineCode.Should().Be("al-2");
            localAuthorityResultSummary.LocalAuthorities.First().Providers.ElementAt(1).FundingPeriods.ElementAt(1).Allocations.First().AllocationLine.AllocationLineName.Should().Be("al 2");
            localAuthorityResultSummary.LocalAuthorities.First().Providers.ElementAt(1).FundingPeriods.ElementAt(1).Allocations.First().AllocationVersionNumber.Should().Be(1);
            localAuthorityResultSummary.LocalAuthorities.First().Providers.ElementAt(1).FundingPeriods.ElementAt(1).Allocations.First().AllocationStatus.Should().Be("Published");
            localAuthorityResultSummary.LocalAuthorities.First().Providers.ElementAt(1).FundingPeriods.ElementAt(1).Allocations.First().AllocationAmount.Should().Be(100);
            localAuthorityResultSummary.LocalAuthorities.First().Providers.ElementAt(2).Ukprn.Should().Be("3333");
            localAuthorityResultSummary.LocalAuthorities.First().Providers.ElementAt(2).OrganisationName.Should().Be("Provider 3");
            localAuthorityResultSummary.LocalAuthorities.First().Providers.ElementAt(2).OrganisationType.Should().Be("Provider Type 1");
            localAuthorityResultSummary.LocalAuthorities.First().Providers.ElementAt(2).OrganisationSubType.Should().Be("Sub Provider Type 1");
            localAuthorityResultSummary.LocalAuthorities.First().Providers.ElementAt(2).AllocationValue.Should().Be(20);
            localAuthorityResultSummary.LocalAuthorities.First().Providers.ElementAt(2).FundingPeriods.Count().Should().Be(3);
            localAuthorityResultSummary.LocalAuthorities.First().Providers.ElementAt(2).FundingPeriods.ElementAt(2).FundingPeriod.PeriodId.Should().Be("fp-3");
            localAuthorityResultSummary.LocalAuthorities.First().Providers.ElementAt(2).FundingPeriods.ElementAt(2).FundingPeriod.PeriodType.Should().Be("fp type 3");
            localAuthorityResultSummary.LocalAuthorities.First().Providers.ElementAt(2).FundingPeriods.ElementAt(2).Allocations.Count().Should().Be(1);
            localAuthorityResultSummary.LocalAuthorities.First().Providers.ElementAt(2).FundingPeriods.ElementAt(2).Allocations.First().AllocationLine.AllocationLineCode.Should().Be("al-3");
            localAuthorityResultSummary.LocalAuthorities.First().Providers.ElementAt(2).FundingPeriods.ElementAt(2).Allocations.First().AllocationLine.AllocationLineName.Should().Be("al 3");
            localAuthorityResultSummary.LocalAuthorities.First().Providers.ElementAt(2).FundingPeriods.ElementAt(2).Allocations.First().AllocationVersionNumber.Should().Be(1);
            localAuthorityResultSummary.LocalAuthorities.First().Providers.ElementAt(2).FundingPeriods.ElementAt(2).Allocations.First().AllocationStatus.Should().Be("Approved");
            localAuthorityResultSummary.LocalAuthorities.First().Providers.ElementAt(2).FundingPeriods.ElementAt(2).Allocations.First().AllocationAmount.Should().Be(20);

            await
                searchService
                    .Received(1)
                    .GetLocalAuthorityFeeds(Arg.Is(laCode), Arg.Is(startYear), Arg.Is(endYear), Arg.Is<IList<string>>(m => m.Count == 1 && m.First() == "allocationLineId eq 'AllocationLine1'"));
        }

        static ProviderResultsService CreateService(IAllocationNotificationsFeedsSearchService searchService = null, ILogger logger = null)
        {
            return new ProviderResultsService(searchService ?? CreateSearchService(), logger ?? CreateLogger());
        }

        static IAllocationNotificationsFeedsSearchService CreateSearchService()
        {
            return Substitute.For<IAllocationNotificationsFeedsSearchService>();
        }

        static ILogger CreateLogger()
        {
            return Substitute.For<ILogger>();
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
                         FundingPeriodEndDate = DateTimeOffset.Now.AddYears(1),
                         FundingPeriodId = "fp-1",
                         FundingPeriodStartDate = DateTimeOffset.Now,
                         FundingPeriodType = "fp type 1",
                         FundingStreamId = "fs-1",
                         FundingStreamName = "fs 1",
                         Id = "id-1",
                         ProviderId = "1111",
                         ProviderUkPrn = "1111",
                         ProviderUpin = "0001",
                         ProviderName = "Provider 1",
                         ProviderType = "Provider Type 1",
                         SubProviderType = "Sub Provider Type 1",
                         LaCode = "123",
                         Authority = "LA 1",
                         Summary = "test summary 1",
                         Title = "test title 1",
                         ProviderProfiling = CreateProfiles(),
                         PolicySummaries = CreatePolicySummaries()
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
                         FundingPeriodEndDate = DateTimeOffset.Now.AddYears(1),
                         FundingPeriodId = "fp-2",
                         FundingPeriodStartDate = DateTimeOffset.Now,
                         FundingPeriodType = "fp type 2",
                         FundingStreamId = "fs-2",
                         FundingStreamName = "fs 2",
                         Id = "id-2",
                         ProviderId = "2222",
                         ProviderUkPrn = "2222",
                         ProviderUpin = "0002",
                         ProviderName = "Provider 2",
                         ProviderType = "Provider Type 1",
                         SubProviderType = "Sub Provider Type 1",
                         LaCode = "123",
                         Authority = "LA 1",
                         Summary = "test summary 2",
                         Title = "test title 2",
                         ProviderProfiling = CreateProfiles(),
                         PolicySummaries = CreatePolicySummaries()
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
                         FundingPeriodEndDate = DateTimeOffset.Now.AddYears(1),
                         FundingPeriodId = "fp-3",
                         FundingPeriodStartDate = DateTimeOffset.Now,
                         FundingPeriodType = "fp type 3",
                         FundingStreamId = "fs-3",
                         FundingStreamName = "fs 3",
                         Id = "id-3",
                         ProviderId = "3333",
                         ProviderUkPrn = "3333",
                         ProviderUpin = "0003",
                         ProviderName = "Provider 3",
                         ProviderType = "Provider Type 1",
                         SubProviderType = "Sub Provider Type 1",
                         LaCode = "123",
                         Authority = "LA 1",
                         Summary = "test summary 3",
                         Title = "test title 3",
                         ProviderProfiling = CreateProfiles(),
                         PolicySummaries = CreatePolicySummaries()
                    }
                };
        }

        static string CreatePolicySummaries()
        {
            var sb = new StringBuilder();
            sb.AppendLine(@"[{""policy"":{""description"":""This is another etst policy created 9th August 2018"",""parentPolicyId"":null,""id"":""239c8b47-89e6-4906-a3bf-866bd11da2f4"",""name"":""Ab Test Policy 0908-001""},""policies"":[{""policy"":{""description"":""Yet another test sub policy"",""parentPolicyId"":null,""id"":""675143bc-84b0-4948-a0cf-bb3a7dd7e1ef"",""name"":""AB Test Sub Policy""},""policies"":[],""calculations"":[]}],""calculations"":[{""calculationName"":""Learner Count"",""calculationVersionNumber"":1,""calculationType"":""Number"",""calculationAmount"":1003.0},{""calculationName"":""AB Test Calc 0908-001"",""calculationVersionNumber"":1,""calculationType"":""Funding"",""calculationAmount"":300.0},{""calculationName"":""AB Test Calc 0908-002"",""calculationVersionNumber"":1,""calculationType"":""Funding"",""calculationAmount"":0.0}]}]");
            return sb.ToString();
        }

        static string CreateProfiles()
        {
            return "[{\"period\":\"Oct\",\"occurrence\":1,\"periodYear\":2017,\"periodType\":\"CalendarMonth\",\"periodValue\":5.5,\"distributionPeriod\":\"2017-2018\"},{\"period\":\"Apr\",\"occurrence\":1,\"periodYear\":2018,\"periodType\":\"CalendarMonth\",\"periodValue\":5.5,\"distributionPeriod\":\"2017-2018\"}]";
        }
    }
}
