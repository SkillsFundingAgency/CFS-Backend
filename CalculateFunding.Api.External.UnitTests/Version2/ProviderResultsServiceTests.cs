using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CalculateFunding.Api.External.V2.Models;
using CalculateFunding.Api.External.V2.Services;
using CalculateFunding.Common.FeatureToggles;
using CalculateFunding.Models.Results.Search;
using CalculateFunding.Models.Search;
using CalculateFunding.Services.Results.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using NSubstitute;
using Serilog;

namespace CalculateFunding.Api.External.UnitTests.Version2
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
                .Warning(Arg.Is($"Feed with id {allocationNotificationFeedIndex.Id} contains missing data: funding period id"));
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
            providerResultSummary.Provider.UkPrn.Should().Be("1111");
            providerResultSummary.FundingPeriodResults.Count().Should().Be(3);
            providerResultSummary.FundingPeriodResults.ElementAt(0).FundingStreamResults.Count().Should().Be(1);
            providerResultSummary.FundingPeriodResults.ElementAt(1).FundingStreamResults.Count().Should().Be(1);
            providerResultSummary.FundingPeriodResults.ElementAt(2).FundingStreamResults.Count().Should().Be(1);
            providerResultSummary.FundingPeriodResults.ElementAt(0).FundingStreamResults.First().FundingStream.Id.Should().Be("fs-1");
            providerResultSummary.FundingPeriodResults.ElementAt(0).FundingStreamResults.First().FundingStream.Name.Should().Be("fs 1");
            providerResultSummary.FundingPeriodResults.ElementAt(0).FundingStreamResults.First().TotalAmount = 10;
            providerResultSummary.FundingPeriodResults.ElementAt(0).FundingStreamResults.First().Allocations.Count().Should().Be(1);
            providerResultSummary.FundingPeriodResults.ElementAt(0).FundingStreamResults.First().Allocations.First().AllocationLine.Id.Should().Be("al-1");
            providerResultSummary.FundingPeriodResults.ElementAt(0).FundingStreamResults.First().Allocations.First().AllocationLine.Name.Should().Be("al 1");
            providerResultSummary.FundingPeriodResults.ElementAt(0).FundingStreamResults.First().Allocations.First().AllocationVersionNumber.Should().Be(1);
            providerResultSummary.FundingPeriodResults.ElementAt(0).FundingStreamResults.First().Allocations.First().AllocationStatus.Should().Be("Published");
            providerResultSummary.FundingPeriodResults.ElementAt(0).FundingStreamResults.First().Allocations.First().AllocationAmount.Should().Be(10);
            providerResultSummary.FundingPeriodResults.ElementAt(0).FundingStreamResults.First().Policies.Count().Should().Be(1);
            providerResultSummary.FundingPeriodResults.ElementAt(0).FundingStreamResults.First().Policies.First().Policy.PolicyId.Should().Be("239c8b47-89e6-4906-a3bf-866bd11da2f4");
            providerResultSummary.FundingPeriodResults.ElementAt(0).FundingStreamResults.First().Policies.First().Policy.PolicyName.Should().Be("Ab Test Policy 0908-001");
            providerResultSummary.FundingPeriodResults.ElementAt(0).FundingStreamResults.First().Policies.First().TotalAmount.Should().Be(300);
            providerResultSummary.FundingPeriodResults.ElementAt(0).FundingStreamResults.First().Policies.First().SubPolicyResults.Count().Should().Be(1);
            providerResultSummary.FundingPeriodResults.ElementAt(0).FundingStreamResults.First().Policies.First().Calculations.Count().Should().Be(3);
            providerResultSummary.FundingPeriodResults.ElementAt(1).FundingStreamResults.First().FundingStream.Id.Should().Be("fs-2");
            providerResultSummary.FundingPeriodResults.ElementAt(1).FundingStreamResults.First().FundingStream.Name.Should().Be("fs 2");
            providerResultSummary.FundingPeriodResults.ElementAt(1).FundingStreamResults.First().TotalAmount = 100;
            providerResultSummary.FundingPeriodResults.ElementAt(1).FundingStreamResults.First().Allocations.Count().Should().Be(1);
            providerResultSummary.FundingPeriodResults.ElementAt(1).FundingStreamResults.First().Allocations.First().AllocationLine.Id.Should().Be("al-2");
            providerResultSummary.FundingPeriodResults.ElementAt(1).FundingStreamResults.First().Allocations.First().AllocationLine.Name.Should().Be("al 2");
            providerResultSummary.FundingPeriodResults.ElementAt(1).FundingStreamResults.First().Allocations.First().AllocationVersionNumber.Should().Be(1);
            providerResultSummary.FundingPeriodResults.ElementAt(1).FundingStreamResults.First().Allocations.First().AllocationStatus.Should().Be("Published");
            providerResultSummary.FundingPeriodResults.ElementAt(1).FundingStreamResults.First().Allocations.First().AllocationAmount.Should().Be(100);
            providerResultSummary.FundingPeriodResults.ElementAt(1).FundingStreamResults.First().Policies.Count().Should().Be(1);
            providerResultSummary.FundingPeriodResults.ElementAt(1).FundingStreamResults.First().Policies.First().Policy.PolicyId.Should().Be("239c8b47-89e6-4906-a3bf-866bd11da2f4");
            providerResultSummary.FundingPeriodResults.ElementAt(1).FundingStreamResults.First().Policies.First().Policy.PolicyName.Should().Be("Ab Test Policy 0908-001");
            providerResultSummary.FundingPeriodResults.ElementAt(1).FundingStreamResults.First().Policies.First().TotalAmount.Should().Be(300);
            providerResultSummary.FundingPeriodResults.ElementAt(1).FundingStreamResults.First().Policies.First().SubPolicyResults.Count().Should().Be(1);
            providerResultSummary.FundingPeriodResults.ElementAt(1).FundingStreamResults.First().Policies.First().Calculations.Count().Should().Be(3);
            providerResultSummary.FundingPeriodResults.ElementAt(2).FundingStreamResults.First().FundingStream.Id.Should().Be("fs-3");
            providerResultSummary.FundingPeriodResults.ElementAt(2).FundingStreamResults.First().FundingStream.Name.Should().Be("fs 3");
            providerResultSummary.FundingPeriodResults.ElementAt(2).FundingStreamResults.First().TotalAmount = 20;
            providerResultSummary.FundingPeriodResults.ElementAt(2).FundingStreamResults.First().Allocations.Count().Should().Be(1);
            providerResultSummary.FundingPeriodResults.ElementAt(2).FundingStreamResults.First().Allocations.First().AllocationLine.Id.Should().Be("al-3");
            providerResultSummary.FundingPeriodResults.ElementAt(2).FundingStreamResults.First().Allocations.First().AllocationLine.Name.Should().Be("al 3");
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
                .Warning(Arg.Is($"Feed with id {allocationNotificationFeedIndex.Id} contains missing data: funding period id"));
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
            providerResultSummary.Provider.UkPrn.Should().Be("1111");
            providerResultSummary.FundingPeriodResults.Count().Should().Be(3);
            providerResultSummary.Provider.Name.Should().Be("Provider 1");
            providerResultSummary.Provider.LegalName.Should().Be("legal 1");
            providerResultSummary.Provider.UkPrn.Should().Be("1111");
            providerResultSummary.Provider.Upin.Should().Be("0001");
            providerResultSummary.Provider.Urn.Should().Be("01");
            providerResultSummary.Provider.DfeEstablishmentNumber.Should().Be("dfe-1");
            providerResultSummary.Provider.EstablishmentNumber.Should().Be("e-1");
            providerResultSummary.Provider.LaCode.Should().Be("123");
            providerResultSummary.Provider.LocalAuthority.Should().Be("LA 1");
            providerResultSummary.Provider.Type.Should().Be("Provider Type 1");
            providerResultSummary.Provider.SubType.Should().Be("Sub Provider Type 1");
            providerResultSummary.Provider.CrmAccountId.Should().Be("crm-1");
            providerResultSummary.Provider.NavVendorNo.Should().Be("nv-1");
            providerResultSummary.Provider.Status.Should().Be("Active");
            providerResultSummary.Provider.OpenDate.Should().NotBeNull();
            providerResultSummary.Provider.CloseDate.Should().NotBeNull();
            providerResultSummary.FundingPeriodResults.ElementAt(0).FundingStreamResults.Count().Should().Be(1);
            providerResultSummary.FundingPeriodResults.ElementAt(1).FundingStreamResults.Count().Should().Be(1);
            providerResultSummary.FundingPeriodResults.ElementAt(2).FundingStreamResults.Count().Should().Be(1);
            providerResultSummary.FundingPeriodResults.ElementAt(0).FundingStreamResults.First().FundingStream.Id.Should().Be("fs-1");
            providerResultSummary.FundingPeriodResults.ElementAt(0).FundingStreamResults.First().FundingStream.Name.Should().Be("fs 1");
            providerResultSummary.FundingPeriodResults.ElementAt(0).FundingStreamResults.First().TotalAmount = 10;
            providerResultSummary.FundingPeriodResults.ElementAt(0).FundingStreamResults.First().Allocations.Count().Should().Be(1);
            providerResultSummary.FundingPeriodResults.ElementAt(0).FundingStreamResults.First().Allocations.First().AllocationLine.Id.Should().Be("al-1");
            providerResultSummary.FundingPeriodResults.ElementAt(0).FundingStreamResults.First().Allocations.First().AllocationLine.Name.Should().Be("al 1");
            providerResultSummary.FundingPeriodResults.ElementAt(0).FundingStreamResults.First().Allocations.First().AllocationVersionNumber.Should().Be(1);
            providerResultSummary.FundingPeriodResults.ElementAt(0).FundingStreamResults.First().Allocations.First().AllocationStatus.Should().Be("Published");
            providerResultSummary.FundingPeriodResults.ElementAt(0).FundingStreamResults.First().Allocations.First().AllocationAmount.Should().Be(10);
            providerResultSummary.FundingPeriodResults.ElementAt(0).FundingStreamResults.First().Policies.Count().Should().Be(1);
            providerResultSummary.FundingPeriodResults.ElementAt(0).FundingStreamResults.First().Policies.First().Policy.PolicyId.Should().Be("239c8b47-89e6-4906-a3bf-866bd11da2f4");
            providerResultSummary.FundingPeriodResults.ElementAt(0).FundingStreamResults.First().Policies.First().Policy.PolicyName.Should().Be("Ab Test Policy 0908-001");
            providerResultSummary.FundingPeriodResults.ElementAt(0).FundingStreamResults.First().Policies.First().TotalAmount.Should().Be(300);
            providerResultSummary.FundingPeriodResults.ElementAt(0).FundingStreamResults.First().Policies.First().SubPolicyResults.Count().Should().Be(1);
            providerResultSummary.FundingPeriodResults.ElementAt(0).FundingStreamResults.First().Policies.First().Calculations.Count().Should().Be(3);
            providerResultSummary.FundingPeriodResults.ElementAt(0).FundingStreamResults.First().FundingStream.ShortName.Should().Be("fs-short-1");
            providerResultSummary.FundingPeriodResults.ElementAt(0).FundingStreamResults.First().FundingStream.PeriodType.Id.Should().Be("fspi-1");
            providerResultSummary.FundingPeriodResults.ElementAt(0).FundingStreamResults.First().FundingStream.PeriodType.Name.Should().Be("fspi1");
            providerResultSummary.FundingPeriodResults.ElementAt(0).FundingStreamResults.First().FundingStream.PeriodType.StartDay.Should().Be(1);
            providerResultSummary.FundingPeriodResults.ElementAt(0).FundingStreamResults.First().FundingStream.PeriodType.StartMonth.Should().Be(8);
            providerResultSummary.FundingPeriodResults.ElementAt(0).FundingStreamResults.First().FundingStream.PeriodType.EndDay.Should().Be(31);
            providerResultSummary.FundingPeriodResults.ElementAt(0).FundingStreamResults.First().FundingStream.PeriodType.EndMonth.Should().Be(7);
            providerResultSummary.FundingPeriodResults.ElementAt(0).FundingStreamResults.First().Allocations.First().AllocationLine.Id.Should().Be("al-1");
            providerResultSummary.FundingPeriodResults.ElementAt(0).FundingStreamResults.First().Allocations.First().AllocationLine.Name.Should().Be("al 1");
            providerResultSummary.FundingPeriodResults.ElementAt(0).FundingStreamResults.First().Allocations.First().AllocationLine.ShortName.Should().Be("short-al1");
            providerResultSummary.FundingPeriodResults.ElementAt(0).FundingStreamResults.First().Allocations.First().AllocationLine.FundingRoute.Should().Be("LA");
            providerResultSummary.FundingPeriodResults.ElementAt(0).FundingStreamResults.First().Allocations.First().AllocationLine.ContractRequired.Should().Be("Y");
            providerResultSummary.FundingPeriodResults.ElementAt(1).FundingStreamResults.First().FundingStream.Id.Should().Be("fs-2");
            providerResultSummary.FundingPeriodResults.ElementAt(1).FundingStreamResults.First().FundingStream.Name.Should().Be("fs 2");
            providerResultSummary.FundingPeriodResults.ElementAt(1).FundingStreamResults.First().TotalAmount = 100;
            providerResultSummary.FundingPeriodResults.ElementAt(1).FundingStreamResults.First().Allocations.Count().Should().Be(1);
            providerResultSummary.FundingPeriodResults.ElementAt(1).FundingStreamResults.First().Allocations.First().AllocationLine.Id.Should().Be("al-2");
            providerResultSummary.FundingPeriodResults.ElementAt(1).FundingStreamResults.First().Allocations.First().AllocationLine.Name.Should().Be("al 2");
            providerResultSummary.FundingPeriodResults.ElementAt(1).FundingStreamResults.First().Allocations.First().AllocationVersionNumber.Should().Be(1);
            providerResultSummary.FundingPeriodResults.ElementAt(1).FundingStreamResults.First().Allocations.First().AllocationStatus.Should().Be("Published");
            providerResultSummary.FundingPeriodResults.ElementAt(1).FundingStreamResults.First().Allocations.First().AllocationAmount.Should().Be(100);
            providerResultSummary.FundingPeriodResults.ElementAt(1).FundingStreamResults.First().Policies.Count().Should().Be(1);
            providerResultSummary.FundingPeriodResults.ElementAt(1).FundingStreamResults.First().Policies.First().Policy.PolicyId.Should().Be("239c8b47-89e6-4906-a3bf-866bd11da2f4");
            providerResultSummary.FundingPeriodResults.ElementAt(1).FundingStreamResults.First().Policies.First().Policy.PolicyName.Should().Be("Ab Test Policy 0908-001");
            providerResultSummary.FundingPeriodResults.ElementAt(1).FundingStreamResults.First().Policies.First().TotalAmount.Should().Be(300);
            providerResultSummary.FundingPeriodResults.ElementAt(1).FundingStreamResults.First().Policies.First().SubPolicyResults.Count().Should().Be(1);
            providerResultSummary.FundingPeriodResults.ElementAt(1).FundingStreamResults.First().Policies.First().Calculations.Count().Should().Be(3);
            providerResultSummary.FundingPeriodResults.ElementAt(1).FundingStreamResults.First().FundingStream.ShortName.Should().Be("fs-short-2");
            providerResultSummary.FundingPeriodResults.ElementAt(1).FundingStreamResults.First().FundingStream.PeriodType.Id.Should().Be("fspi-2");
            providerResultSummary.FundingPeriodResults.ElementAt(1).FundingStreamResults.First().FundingStream.PeriodType.Name.Should().Be("fspi2");
            providerResultSummary.FundingPeriodResults.ElementAt(1).FundingStreamResults.First().FundingStream.PeriodType.StartDay.Should().Be(1);
            providerResultSummary.FundingPeriodResults.ElementAt(1).FundingStreamResults.First().FundingStream.PeriodType.StartMonth.Should().Be(8);
            providerResultSummary.FundingPeriodResults.ElementAt(1).FundingStreamResults.First().FundingStream.PeriodType.EndDay.Should().Be(31);
            providerResultSummary.FundingPeriodResults.ElementAt(1).FundingStreamResults.First().FundingStream.PeriodType.EndMonth.Should().Be(7);
            providerResultSummary.FundingPeriodResults.ElementAt(1).FundingStreamResults.First().Allocations.First().AllocationLine.Id.Should().Be("al-2");
            providerResultSummary.FundingPeriodResults.ElementAt(1).FundingStreamResults.First().Allocations.First().AllocationLine.Name.Should().Be("al 2");
            providerResultSummary.FundingPeriodResults.ElementAt(1).FundingStreamResults.First().Allocations.First().AllocationLine.ShortName.Should().Be("short-al2");
            providerResultSummary.FundingPeriodResults.ElementAt(1).FundingStreamResults.First().Allocations.First().AllocationLine.FundingRoute.Should().Be("LA");
            providerResultSummary.FundingPeriodResults.ElementAt(1).FundingStreamResults.First().Allocations.First().AllocationLine.ContractRequired.Should().Be("Y");
            providerResultSummary.FundingPeriodResults.ElementAt(2).FundingStreamResults.First().FundingStream.Id.Should().Be("fs-3");
            providerResultSummary.FundingPeriodResults.ElementAt(2).FundingStreamResults.First().FundingStream.Name.Should().Be("fs 3");
            providerResultSummary.FundingPeriodResults.ElementAt(2).FundingStreamResults.First().TotalAmount = 20;
            providerResultSummary.FundingPeriodResults.ElementAt(2).FundingStreamResults.First().Allocations.Count().Should().Be(1);
            providerResultSummary.FundingPeriodResults.ElementAt(2).FundingStreamResults.First().Allocations.First().AllocationLine.Id.Should().Be("al-3");
            providerResultSummary.FundingPeriodResults.ElementAt(2).FundingStreamResults.First().Allocations.First().AllocationLine.Name.Should().Be("al 3");
            providerResultSummary.FundingPeriodResults.ElementAt(2).FundingStreamResults.First().Allocations.First().AllocationVersionNumber.Should().Be(1);
            providerResultSummary.FundingPeriodResults.ElementAt(2).FundingStreamResults.First().Allocations.First().AllocationStatus.Should().Be("Approved");
            providerResultSummary.FundingPeriodResults.ElementAt(2).FundingStreamResults.First().Allocations.First().AllocationAmount.Should().Be(20);
            providerResultSummary.FundingPeriodResults.ElementAt(2).FundingStreamResults.First().Policies.Count().Should().Be(1);
            providerResultSummary.FundingPeriodResults.ElementAt(2).FundingStreamResults.First().Policies.First().Policy.PolicyId.Should().Be("239c8b47-89e6-4906-a3bf-866bd11da2f4");
            providerResultSummary.FundingPeriodResults.ElementAt(2).FundingStreamResults.First().Policies.First().Policy.PolicyName.Should().Be("Ab Test Policy 0908-001");
            providerResultSummary.FundingPeriodResults.ElementAt(2).FundingStreamResults.First().Policies.First().TotalAmount.Should().Be(300);
            providerResultSummary.FundingPeriodResults.ElementAt(2).FundingStreamResults.First().Policies.First().SubPolicyResults.Count().Should().Be(1);
            providerResultSummary.FundingPeriodResults.ElementAt(2).FundingStreamResults.First().Policies.First().Calculations.Count().Should().Be(3);
            providerResultSummary.FundingPeriodResults.ElementAt(2).FundingStreamResults.First().FundingStream.ShortName.Should().Be("fs-short-3");
            providerResultSummary.FundingPeriodResults.ElementAt(2).FundingStreamResults.First().FundingStream.PeriodType.Id.Should().Be("fspi-3");
            providerResultSummary.FundingPeriodResults.ElementAt(2).FundingStreamResults.First().FundingStream.PeriodType.Name.Should().Be("fspi3");
            providerResultSummary.FundingPeriodResults.ElementAt(2).FundingStreamResults.First().FundingStream.PeriodType.StartDay.Should().Be(1);
            providerResultSummary.FundingPeriodResults.ElementAt(2).FundingStreamResults.First().FundingStream.PeriodType.StartMonth.Should().Be(8);
            providerResultSummary.FundingPeriodResults.ElementAt(2).FundingStreamResults.First().FundingStream.PeriodType.EndDay.Should().Be(31);
            providerResultSummary.FundingPeriodResults.ElementAt(2).FundingStreamResults.First().FundingStream.PeriodType.EndMonth.Should().Be(7);
            providerResultSummary.FundingPeriodResults.ElementAt(2).FundingStreamResults.First().Allocations.First().AllocationLine.Id.Should().Be("al-3");
            providerResultSummary.FundingPeriodResults.ElementAt(2).FundingStreamResults.First().Allocations.First().AllocationLine.Name.Should().Be("al 3");
            providerResultSummary.FundingPeriodResults.ElementAt(2).FundingStreamResults.First().Allocations.First().AllocationLine.ShortName.Should().Be("short-al3");
            providerResultSummary.FundingPeriodResults.ElementAt(2).FundingStreamResults.First().Allocations.First().AllocationLine.FundingRoute.Should().Be("LA");
            providerResultSummary.FundingPeriodResults.ElementAt(2).FundingStreamResults.First().Allocations.First().AllocationLine.ContractRequired.Should().Be("Y");


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
                .Warning(Arg.Is($"Feed with id {allocationNotificationFeedIndex.Id} contains missing data: funding period id"));
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
            localAuthorityResultSummary.LocalAuthorities.First().Providers.ElementAt(0).Provider.UkPrn.Should().Be("1111");
            localAuthorityResultSummary.LocalAuthorities.First().Providers.ElementAt(0).Provider.Name.Should().Be("Provider 1");
            localAuthorityResultSummary.LocalAuthorities.First().Providers.ElementAt(0).Provider.Type.Should().Be("Provider Type 1");
            localAuthorityResultSummary.LocalAuthorities.First().Providers.ElementAt(0).Provider.SubType.Should().Be("Sub Provider Type 1");
            localAuthorityResultSummary.LocalAuthorities.First().Providers.ElementAt(0).Provider.LegalName.Should().Be("legal 1");
            localAuthorityResultSummary.LocalAuthorities.First().Providers.ElementAt(0).Provider.Upin.Should().Be("0001");
            localAuthorityResultSummary.LocalAuthorities.First().Providers.ElementAt(0).Provider.Urn.Should().Be("01");
            localAuthorityResultSummary.LocalAuthorities.First().Providers.ElementAt(0).Provider.DfeEstablishmentNumber.Should().Be("dfe-1");
            localAuthorityResultSummary.LocalAuthorities.First().Providers.ElementAt(0).Provider.EstablishmentNumber.Should().Be("e-1");
            localAuthorityResultSummary.LocalAuthorities.First().Providers.ElementAt(0).Provider.LaCode.Should().Be("123");
            localAuthorityResultSummary.LocalAuthorities.First().Providers.ElementAt(0).Provider.LocalAuthority.Should().Be("LA 1");
            localAuthorityResultSummary.LocalAuthorities.First().Providers.ElementAt(0).Provider.CrmAccountId.Should().Be("crm-1");
            localAuthorityResultSummary.LocalAuthorities.First().Providers.ElementAt(0).Provider.NavVendorNo.Should().Be("nv-1");
            localAuthorityResultSummary.LocalAuthorities.First().Providers.ElementAt(0).Provider.Status.Should().Be("Active");
            localAuthorityResultSummary.LocalAuthorities.First().Providers.ElementAt(0).AllocationValue.Should().Be(10);
            localAuthorityResultSummary.LocalAuthorities.First().Providers.ElementAt(0).FundingPeriods.Count().Should().Be(3);
            localAuthorityResultSummary.LocalAuthorities.First().Providers.ElementAt(0).FundingPeriods.First().Period.Id.Should().Be("fp-1");
            localAuthorityResultSummary.LocalAuthorities.First().Providers.ElementAt(0).FundingPeriods.First().Allocations.Count().Should().Be(1);
            localAuthorityResultSummary.LocalAuthorities.First().Providers.ElementAt(0).FundingPeriods.First().Allocations.First().AllocationLine.Id.Should().Be("al-1");
            localAuthorityResultSummary.LocalAuthorities.First().Providers.ElementAt(0).FundingPeriods.First().Allocations.First().AllocationLine.Name.Should().Be("al 1");
            localAuthorityResultSummary.LocalAuthorities.First().Providers.ElementAt(0).FundingPeriods.First().Allocations.First().AllocationVersionNumber.Should().Be(1);
            localAuthorityResultSummary.LocalAuthorities.First().Providers.ElementAt(0).FundingPeriods.First().Allocations.First().AllocationStatus.Should().Be("Published");
            localAuthorityResultSummary.LocalAuthorities.First().Providers.ElementAt(0).FundingPeriods.First().Allocations.First().AllocationAmount.Should().Be(10);
            localAuthorityResultSummary.LocalAuthorities.First().Providers.ElementAt(1).Provider.UkPrn.Should().Be("2222");
            localAuthorityResultSummary.LocalAuthorities.First().Providers.ElementAt(1).Provider.Name.Should().Be("Provider 2");
            localAuthorityResultSummary.LocalAuthorities.First().Providers.ElementAt(1).Provider.Type.Should().Be("Provider Type 1");
            localAuthorityResultSummary.LocalAuthorities.First().Providers.ElementAt(1).Provider.SubType.Should().Be("Sub Provider Type 1");
            localAuthorityResultSummary.LocalAuthorities.First().Providers.ElementAt(1).Provider.LegalName.Should().Be("legal 2");
            localAuthorityResultSummary.LocalAuthorities.First().Providers.ElementAt(1).Provider.Upin.Should().Be("0002");
            localAuthorityResultSummary.LocalAuthorities.First().Providers.ElementAt(1).Provider.Urn.Should().Be("02");
            localAuthorityResultSummary.LocalAuthorities.First().Providers.ElementAt(1).Provider.DfeEstablishmentNumber.Should().Be("dfe-2");
            localAuthorityResultSummary.LocalAuthorities.First().Providers.ElementAt(1).Provider.EstablishmentNumber.Should().Be("e-2");
            localAuthorityResultSummary.LocalAuthorities.First().Providers.ElementAt(1).Provider.LaCode.Should().Be("123");
            localAuthorityResultSummary.LocalAuthorities.First().Providers.ElementAt(1).Provider.LocalAuthority.Should().Be("LA 1");
            localAuthorityResultSummary.LocalAuthorities.First().Providers.ElementAt(1).Provider.CrmAccountId.Should().Be("crm-2");
            localAuthorityResultSummary.LocalAuthorities.First().Providers.ElementAt(1).Provider.NavVendorNo.Should().Be("nv-2");
            localAuthorityResultSummary.LocalAuthorities.First().Providers.ElementAt(1).Provider.Status.Should().Be("Active");
            localAuthorityResultSummary.LocalAuthorities.First().Providers.ElementAt(1).AllocationValue.Should().Be(100);
            localAuthorityResultSummary.LocalAuthorities.First().Providers.ElementAt(1).FundingPeriods.Count().Should().Be(3);
            localAuthorityResultSummary.LocalAuthorities.First().Providers.ElementAt(1).FundingPeriods.ElementAt(1).Period.Id.Should().Be("fp-2");
            localAuthorityResultSummary.LocalAuthorities.First().Providers.ElementAt(1).FundingPeriods.ElementAt(1).Allocations.Count().Should().Be(1);
            localAuthorityResultSummary.LocalAuthorities.First().Providers.ElementAt(1).FundingPeriods.ElementAt(1).Allocations.First().AllocationLine.Id.Should().Be("al-2");
            localAuthorityResultSummary.LocalAuthorities.First().Providers.ElementAt(1).FundingPeriods.ElementAt(1).Allocations.First().AllocationLine.Name.Should().Be("al 2");
            localAuthorityResultSummary.LocalAuthorities.First().Providers.ElementAt(1).FundingPeriods.ElementAt(1).Allocations.First().AllocationVersionNumber.Should().Be(1);
            localAuthorityResultSummary.LocalAuthorities.First().Providers.ElementAt(1).FundingPeriods.ElementAt(1).Allocations.First().AllocationStatus.Should().Be("Published");
            localAuthorityResultSummary.LocalAuthorities.First().Providers.ElementAt(1).FundingPeriods.ElementAt(1).Allocations.First().AllocationAmount.Should().Be(100);
            localAuthorityResultSummary.LocalAuthorities.First().Providers.ElementAt(2).Provider.UkPrn.Should().Be("3333");
            localAuthorityResultSummary.LocalAuthorities.First().Providers.ElementAt(2).Provider.Name.Should().Be("Provider 3");
            localAuthorityResultSummary.LocalAuthorities.First().Providers.ElementAt(2).Provider.Type.Should().Be("Provider Type 1");
            localAuthorityResultSummary.LocalAuthorities.First().Providers.ElementAt(2).Provider.SubType.Should().Be("Sub Provider Type 1");
            localAuthorityResultSummary.LocalAuthorities.First().Providers.ElementAt(2).Provider.LegalName.Should().Be("legal 3");
            localAuthorityResultSummary.LocalAuthorities.First().Providers.ElementAt(2).Provider.Upin.Should().Be("0003");
            localAuthorityResultSummary.LocalAuthorities.First().Providers.ElementAt(2).Provider.Urn.Should().Be("03");
            localAuthorityResultSummary.LocalAuthorities.First().Providers.ElementAt(2).Provider.DfeEstablishmentNumber.Should().Be("dfe-3");
            localAuthorityResultSummary.LocalAuthorities.First().Providers.ElementAt(2).Provider.EstablishmentNumber.Should().Be("e-3");
            localAuthorityResultSummary.LocalAuthorities.First().Providers.ElementAt(2).Provider.LaCode.Should().Be("123");
            localAuthorityResultSummary.LocalAuthorities.First().Providers.ElementAt(2).Provider.LocalAuthority.Should().Be("LA 1");
            localAuthorityResultSummary.LocalAuthorities.First().Providers.ElementAt(2).Provider.CrmAccountId.Should().Be("crm-3");
            localAuthorityResultSummary.LocalAuthorities.First().Providers.ElementAt(2).Provider.NavVendorNo.Should().Be("nv-3");
            localAuthorityResultSummary.LocalAuthorities.First().Providers.ElementAt(2).Provider.Status.Should().Be("Active");
            localAuthorityResultSummary.LocalAuthorities.First().Providers.ElementAt(2).AllocationValue.Should().Be(20);
            localAuthorityResultSummary.LocalAuthorities.First().Providers.ElementAt(2).FundingPeriods.Count().Should().Be(3);
            localAuthorityResultSummary.LocalAuthorities.First().Providers.ElementAt(2).FundingPeriods.ElementAt(2).Period.Id.Should().Be("fp-3");
            localAuthorityResultSummary.LocalAuthorities.First().Providers.ElementAt(2).FundingPeriods.ElementAt(2).Allocations.Count().Should().Be(1);
            localAuthorityResultSummary.LocalAuthorities.First().Providers.ElementAt(2).FundingPeriods.ElementAt(2).Allocations.First().AllocationLine.Id.Should().Be("al-3");
            localAuthorityResultSummary.LocalAuthorities.First().Providers.ElementAt(2).FundingPeriods.ElementAt(2).Allocations.First().AllocationLine.Name.Should().Be("al 3");
            localAuthorityResultSummary.LocalAuthorities.First().Providers.ElementAt(2).FundingPeriods.ElementAt(2).Allocations.First().AllocationVersionNumber.Should().Be(1);
            localAuthorityResultSummary.LocalAuthorities.First().Providers.ElementAt(2).FundingPeriods.ElementAt(2).Allocations.First().AllocationStatus.Should().Be("Approved");
            localAuthorityResultSummary.LocalAuthorities.First().Providers.ElementAt(2).FundingPeriods.ElementAt(2).Allocations.First().AllocationAmount.Should().Be(20);

            await
                searchService
                    .Received(1)
                    .GetLocalAuthorityFeeds(Arg.Is(laCode), Arg.Is(startYear), Arg.Is(endYear), Arg.Is<IList<string>>(m => m.Count == 1 && m.First() == "allocationLineId eq 'AllocationLine1'"));
        }

        [TestMethod]
        public async Task GetLocalAuthorityProvidersResultsForAllocations_GivenValidResultsFoundFromSearchAndfeatureToggleIsDisabled_ReturnsResults()
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

            feeds.Entries.ElementAt(0).MajorVersion = 1;
            feeds.Entries.ElementAt(0).MinorVersion = 1;

            IFeatureToggle featureToggle = CreateFeatureToggle();
            featureToggle
                .IsAllocationLineMajorMinorVersioningEnabled()
                .Returns(false);

            IAllocationNotificationsFeedsSearchService searchService = CreateSearchService();
            searchService
                .GetLocalAuthorityFeeds(Arg.Is(laCode), Arg.Is(startYear), Arg.Is(endYear), Arg.Any<IEnumerable<string>>())
                .Returns(feeds);

            ProviderResultsService providerResultsService = CreateService(searchService, featureToggle: featureToggle);

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

            localAuthorityResultSummary.LocalAuthorities.First().Providers.ElementAt(0).FundingPeriods.ElementAt(0).Allocations.First().AllocationMajorVersion.Should().Be(0);
            localAuthorityResultSummary.LocalAuthorities.First().Providers.ElementAt(0).FundingPeriods.ElementAt(0).Allocations.First().AllocationMinorVersion.Should().Be(0);

            await
                searchService
                    .Received(1)
                    .GetLocalAuthorityFeeds(Arg.Is(laCode), Arg.Is(startYear), Arg.Is(endYear), Arg.Is<IList<string>>(m => m.Count == 1 && m.First() == "allocationLineId eq 'AllocationLine1'"));
        }

        [TestMethod]
        public async Task GetLocalAuthorityProvidersResultsForAllocations_GivenValidResultsFoundFromSearchAndfeatureToggleIsEnabled_ReturnsResults()
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

            feeds.Entries.ElementAt(0).MajorVersion = 1;
            feeds.Entries.ElementAt(1).MajorVersion = 1;

            IFeatureToggle featureToggle = CreateFeatureToggle();
            featureToggle
                .IsAllocationLineMajorMinorVersioningEnabled()
                .Returns(true);

            IAllocationNotificationsFeedsSearchService searchService = CreateSearchService();
            searchService
                .GetLocalAuthorityFeeds(Arg.Is(laCode), Arg.Is(startYear), Arg.Is(endYear), Arg.Any<IEnumerable<string>>())
                .Returns(feeds);

            ProviderResultsService providerResultsService = CreateService(searchService, featureToggle: featureToggle);

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

            localAuthorityResultSummary.LocalAuthorities.First().Providers.ElementAt(0).FundingPeriods.ElementAt(0).Allocations.First().AllocationMajorVersion.Should().Be(1);
            localAuthorityResultSummary.LocalAuthorities.First().Providers.ElementAt(0).FundingPeriods.ElementAt(0).Allocations.First().AllocationMinorVersion.Should().Be(0);

            await
                searchService
                    .Received(1)
                    .GetLocalAuthorityFeeds(Arg.Is(laCode), Arg.Is(startYear), Arg.Is(endYear), Arg.Is<IList<string>>(m => m.Count == 1 && m.First() == "allocationLineId eq 'AllocationLine1'"));
        }

        static ProviderResultsService CreateService(IAllocationNotificationsFeedsSearchService searchService = null, ILogger logger = null, IFeatureToggle featureToggle = null)
        {
            return new ProviderResultsService(searchService ?? CreateSearchService(), logger ?? CreateLogger(), featureToggle ?? CreateFeatureToggle());
        }

        static IFeatureToggle CreateFeatureToggle()
        {
            return Substitute.For<IFeatureToggle>();
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
                         FundingPeriodEndYear = DateTimeOffset.Now.AddYears(1).Year,
                         FundingPeriodId = "fp-1",
                         FundingPeriodStartYear = DateTimeOffset.Now.Year,
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
                         AllocationLineContractRequired = true,
                         AllocationLineFundingRoute = "LA",
                         AllocationLineShortName = "short-al1",
                         CrmAccountId = "crm-1",
                         DfeEstablishmentNumber = "dfe-1",
                         EstablishmentNumber = "e-1",
                         FundingStreamEndDay = 31,
                         FundingStreamEndMonth = 7,
                         FundingStreamPeriodId = "fspi-1",
                         FundingStreamPeriodName = "fspi1",
                         FundingStreamShortName = "fs-short-1",
                         FundingStreamStartDay = 1,
                         FundingStreamStartMonth = 8,
                         NavVendorNo = "nv-1",
                         ProviderClosedDate = DateTimeOffset.Now,
                         ProviderLegalName = "legal 1",
                         ProviderOpenDate = DateTimeOffset.Now,
                         ProviderStatus = "Active",
                         ProviderUrn = "01",
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
                         FundingPeriodEndYear = DateTimeOffset.Now.AddYears(1).Year,
                         FundingPeriodId = "fp-2",
                         FundingPeriodStartYear = DateTimeOffset.Now.Year,
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
                         AllocationLineContractRequired = true,
                         AllocationLineFundingRoute = "LA",
                         AllocationLineShortName = "short-al2",
                         CrmAccountId = "crm-2",
                         DfeEstablishmentNumber = "dfe-2",
                         EstablishmentNumber = "e-2",
                         FundingStreamEndDay = 31,
                         FundingStreamEndMonth = 7,
                         FundingStreamPeriodId = "fspi-2",
                         FundingStreamPeriodName = "fspi2",
                         FundingStreamShortName = "fs-short-2",
                         FundingStreamStartDay = 1,
                         FundingStreamStartMonth = 8,
                         NavVendorNo = "nv-2",
                         ProviderClosedDate = DateTimeOffset.Now,
                         ProviderLegalName = "legal 2",
                         ProviderOpenDate = DateTimeOffset.Now,
                         ProviderStatus = "Active",
                         ProviderUrn = "02",
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
                         FundingPeriodEndYear = DateTimeOffset.Now.AddYears(1).Year,
                         FundingPeriodId = "fp-3",
                         FundingPeriodStartYear = DateTimeOffset.Now.Year,
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
                         AllocationLineContractRequired = true,
                         AllocationLineFundingRoute = "LA",
                         AllocationLineShortName = "short-al3",
                         CrmAccountId = "crm-3",
                         DfeEstablishmentNumber = "dfe-3",
                         EstablishmentNumber = "e-3",
                         FundingStreamEndDay = 31,
                         FundingStreamEndMonth = 7,
                         FundingStreamPeriodId = "fspi-3",
                         FundingStreamPeriodName = "fspi3",
                         FundingStreamShortName = "fs-short-3",
                         FundingStreamStartDay = 1,
                         FundingStreamStartMonth = 8,
                         NavVendorNo = "nv-3",
                         ProviderClosedDate = DateTimeOffset.Now,
                         ProviderLegalName = "legal 3",
                         ProviderOpenDate = DateTimeOffset.Now,
                         ProviderStatus = "Active",
                         ProviderUrn = "03",
                         ProviderProfiling = CreateProfiles(),
                         PolicySummaries = CreatePolicySummaries()
                    }
                };
        }

        static string CreatePolicySummaries()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(@"[{""policy"":{""description"":""This is another etst policy created 9th August 2018"",""parentPolicyId"":null,""id"":""239c8b47-89e6-4906-a3bf-866bd11da2f4"",""name"":""Ab Test Policy 0908-001""},""policies"":[{""policy"":{""description"":""Yet another test sub policy"",""parentPolicyId"":null,""id"":""675143bc-84b0-4948-a0cf-bb3a7dd7e1ef"",""name"":""AB Test Sub Policy""},""policies"":[],""calculations"":[]}],""calculations"":[{""calculationName"":""Learner Count"",""calculationVersionNumber"":1,""calculationType"":""Number"",""calculationAmount"":1003.0},{""calculationName"":""AB Test Calc 0908-001"",""calculationVersionNumber"":1,""calculationType"":""Funding"",""calculationAmount"":300.0},{""calculationName"":""AB Test Calc 0908-002"",""calculationVersionNumber"":1,""calculationType"":""Funding"",""calculationAmount"":0.0}]}]");
            return sb.ToString();
        }

        static string CreateProfiles()
        {
            return "[{\"period\":\"Oct\",\"occurrence\":1,\"periodYear\":2017,\"periodType\":\"CalendarMonth\",\"periodValue\":5.5,\"distributionPeriod\":\"2017-2018\"},{\"period\":\"Apr\",\"occurrence\":1,\"periodYear\":2018,\"periodType\":\"CalendarMonth\",\"periodValue\":5.5,\"distributionPeriod\":\"2017-2018\"}]";
        }
    }
}
