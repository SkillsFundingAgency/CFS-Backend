using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Api.External.V2.Models;
using CalculateFunding.Api.External.V2.Services;
using CalculateFunding.Common.FeatureToggles;
using CalculateFunding.Models.External.AtomItems;
using CalculateFunding.Models.Results;
using CalculateFunding.Models.Search;
using CalculateFunding.Services.Results.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using NSubstitute;

namespace CalculateFunding.Api.External.UnitTests.Version2
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
            IActionResult result = await service.GetNotifications(null, "", null, request);

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
            IActionResult result = await service.GetNotifications(3, "", null, request);

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
            IActionResult result = await service.GetNotifications(3, "Published,Approved", null, request);

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
            IActionResult result = await service.GetNotifications(-1, "", null, request);

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
        public async Task GetNotifications_GivenInvallidPageSizeOfZero_ReturnsBadRequest()
        {
            //Arrange
            AllocationNotificationFeedsService service = CreateService();

            HttpRequest request = Substitute.For<HttpRequest>();

            //Act
            IActionResult result = await service.GetNotifications(1, "", 0, request);

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
        public async Task GetNotifications_GivenInvallidPageSizeOfThousand_ReturnsBadRequest()
        {
            //Arrange
            AllocationNotificationFeedsService service = CreateService();

            HttpRequest request = Substitute.For<HttpRequest>();

            //Act
            IActionResult result = await service.GetNotifications(1, "", 1000, request);

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
            IActionResult result = await service.GetNotifications(3, "Published,Approved", null, request);

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
                .GetFeeds(Arg.Is(3), Arg.Is(2), Arg.Any<IEnumerable<string>>())
                .Returns(feeds);

            AllocationNotificationFeedsService service = CreateService(feedsSearchService);

            IHeaderDictionary headerDictionary = new HeaderDictionary();
            headerDictionary.Add("Accept", new StringValues("application/json"));

            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "pageRef", new StringValues("3") },
                { "allocationStatuses", new StringValues("Published,Approved") },
                { "pageSize", new StringValues("2") }
            });

            HttpRequest request = Substitute.For<HttpRequest>();
            request.Scheme.Returns("https");
            request.Path.Returns(new PathString("/api/v1/test"));
            request.Host.Returns(new HostString("wherever.naf:12345"));
            request.QueryString.Returns(new QueryString("?pageRef=3&pageSize=2&allocationStatuses=Published,Approved"));
            request.Headers.Returns(headerDictionary);
            request.Query.Returns(queryStringValues);

            //Act
            IActionResult result = await service.GetNotifications(3, "Published,Approved", 2, request);

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
            atomFeed.Link.First(m => m.Rel == "self").Href.Should().Be("https://wherever.naf:12345/api/v1/notifications?pageRef=3&allocationStatuses=Published,Approved&pageSize=2");
            atomFeed.Link.First(m => m.Rel == "first").Href.Should().Be("https://wherever.naf:12345/api/v1/notifications?pageRef=1&allocationStatuses=Published,Approved&pageSize=2");
            atomFeed.Link.First(m => m.Rel == "last").Href.Should().Be("https://wherever.naf:12345/api/v1/notifications?pageRef=1&allocationStatuses=Published,Approved&pageSize=2");
            atomFeed.Link.First(m => m.Rel == "previous").Href.Should().Be("https://wherever.naf:12345/api/v1/notifications?pageRef=2&allocationStatuses=Published,Approved&pageSize=2");
            atomFeed.Link.First(m => m.Rel == "next").Href.Should().Be("https://wherever.naf:12345/api/v1/notifications?pageRef=3&allocationStatuses=Published,Approved&pageSize=2");
            atomFeed.AtomEntry.Count.Should().Be(3);
            atomFeed.AtomEntry.ElementAt(0).Id.Should().Be("id-1");
            atomFeed.AtomEntry.ElementAt(0).Title.Should().Be("test title 1");
            atomFeed.AtomEntry.ElementAt(0).Summary.Should().Be("test summary 1");
            atomFeed.AtomEntry.ElementAt(0).Content.Allocation.FundingStream.Id.Should().Be("fs-1");
            atomFeed.AtomEntry.ElementAt(0).Content.Allocation.FundingStream.Name.Should().Be("fs 1");
            atomFeed.AtomEntry.ElementAt(0).Content.Allocation.Period.Id.Should().Be("fp-1");
            atomFeed.AtomEntry.ElementAt(0).Content.Allocation.Provider.UkPrn.Should().Be("1111");
            atomFeed.AtomEntry.ElementAt(0).Content.Allocation.Provider.Upin.Should().Be("0001");
            atomFeed.AtomEntry.ElementAt(0).Content.Allocation.AllocationLine.Id.Should().Be("al-1");
            atomFeed.AtomEntry.ElementAt(0).Content.Allocation.AllocationLine.Name.Should().Be("al 1");
            atomFeed.AtomEntry.ElementAt(0).Content.Allocation.AllocationVersionNumber.Should().Be(1);
            atomFeed.AtomEntry.ElementAt(0).Content.Allocation.AllocationStatus.Should().Be("Published");
            atomFeed.AtomEntry.ElementAt(0).Content.Allocation.AllocationAmount.Should().Be(10);
            atomFeed.AtomEntry.ElementAt(0).Content.Allocation.ProfilePeriods.Length.Should().Be(0);
            atomFeed.AtomEntry.ElementAt(0).Content.Allocation.FundingStream.ShortName.Should().Be("fs-short-1");
            atomFeed.AtomEntry.ElementAt(0).Content.Allocation.FundingStream.PeriodType.Id.Should().Be("fspi-1");
            atomFeed.AtomEntry.ElementAt(0).Content.Allocation.FundingStream.PeriodType.Name.Should().Be("fspi1");
            atomFeed.AtomEntry.ElementAt(0).Content.Allocation.FundingStream.PeriodType.StartDay.Should().Be(1);
            atomFeed.AtomEntry.ElementAt(0).Content.Allocation.FundingStream.PeriodType.StartMonth.Should().Be(8);
            atomFeed.AtomEntry.ElementAt(0).Content.Allocation.FundingStream.PeriodType.EndDay.Should().Be(31);
            atomFeed.AtomEntry.ElementAt(0).Content.Allocation.FundingStream.PeriodType.EndMonth.Should().Be(7);
            atomFeed.AtomEntry.ElementAt(0).Content.Allocation.Period.Name.Should().Be("fspi1");
            atomFeed.AtomEntry.ElementAt(0).Content.Allocation.Period.StartYear.Should().Be(2018);
            atomFeed.AtomEntry.ElementAt(0).Content.Allocation.Period.EndYear.Should().Be(2019);
            atomFeed.AtomEntry.ElementAt(0).Content.Allocation.Provider.Name.Should().Be("provider 1");
            atomFeed.AtomEntry.ElementAt(0).Content.Allocation.Provider.LegalName.Should().Be("legal 1");
            atomFeed.AtomEntry.ElementAt(0).Content.Allocation.Provider.Urn.Should().Be("01");
            atomFeed.AtomEntry.ElementAt(0).Content.Allocation.Provider.DfeEstablishmentNumber.Should().Be("dfe-1");
            atomFeed.AtomEntry.ElementAt(0).Content.Allocation.Provider.EstablishmentNumber.Should().Be("e-1");
            atomFeed.AtomEntry.ElementAt(0).Content.Allocation.Provider.LaCode.Should().Be("la-1");
            atomFeed.AtomEntry.ElementAt(0).Content.Allocation.Provider.LocalAuthority.Should().Be("authority");
            atomFeed.AtomEntry.ElementAt(0).Content.Allocation.Provider.Type.Should().Be("type 1");
            atomFeed.AtomEntry.ElementAt(0).Content.Allocation.Provider.SubType.Should().Be("sub type 1");
            atomFeed.AtomEntry.ElementAt(0).Content.Allocation.Provider.OpenDate.Should().NotBeNull();
            atomFeed.AtomEntry.ElementAt(0).Content.Allocation.Provider.CloseDate.Should().NotBeNull();
            atomFeed.AtomEntry.ElementAt(0).Content.Allocation.Provider.CrmAccountId.Should().Be("crm-1");
            atomFeed.AtomEntry.ElementAt(0).Content.Allocation.Provider.NavVendorNo.Should().Be("nv-1");
            atomFeed.AtomEntry.ElementAt(0).Content.Allocation.Provider.Status.Should().Be("Active");
            atomFeed.AtomEntry.ElementAt(0).Content.Allocation.AllocationLine.ShortName.Should().Be("short-al1");
            atomFeed.AtomEntry.ElementAt(0).Content.Allocation.AllocationLine.FundingRoute.Should().Be("LA");
            atomFeed.AtomEntry.ElementAt(0).Content.Allocation.AllocationLine.ContractRequired.Should().Be("Y");
            atomFeed.AtomEntry.ElementAt(1).Id.Should().Be("id-2");
            atomFeed.AtomEntry.ElementAt(1).Title.Should().Be("test title 2");
            atomFeed.AtomEntry.ElementAt(1).Summary.Should().Be("test summary 2");
            atomFeed.AtomEntry.ElementAt(1).Content.Allocation.FundingStream.Id.Should().Be("fs-2");
            atomFeed.AtomEntry.ElementAt(1).Content.Allocation.FundingStream.Name.Should().Be("fs 2");
            atomFeed.AtomEntry.ElementAt(1).Content.Allocation.Period.Id.Should().Be("fp-2");
            atomFeed.AtomEntry.ElementAt(1).Content.Allocation.Provider.UkPrn.Should().Be("2222");
            atomFeed.AtomEntry.ElementAt(1).Content.Allocation.Provider.Upin.Should().Be("0002");
            atomFeed.AtomEntry.ElementAt(1).Content.Allocation.AllocationLine.Id.Should().Be("al-2");
            atomFeed.AtomEntry.ElementAt(1).Content.Allocation.AllocationLine.Name.Should().Be("al 2");
            atomFeed.AtomEntry.ElementAt(1).Content.Allocation.AllocationVersionNumber.Should().Be(1);
            atomFeed.AtomEntry.ElementAt(1).Content.Allocation.AllocationStatus.Should().Be("Published");
            atomFeed.AtomEntry.ElementAt(1).Content.Allocation.AllocationAmount.Should().Be(100);
            atomFeed.AtomEntry.ElementAt(1).Content.Allocation.ProfilePeriods.Length.Should().Be(2);
            atomFeed.AtomEntry.ElementAt(1).Content.Allocation.FundingStream.ShortName.Should().Be("fs-short-2");
            atomFeed.AtomEntry.ElementAt(1).Content.Allocation.FundingStream.PeriodType.Id.Should().Be("fspi-2");
            atomFeed.AtomEntry.ElementAt(1).Content.Allocation.FundingStream.PeriodType.Name.Should().Be("fspi2");
            atomFeed.AtomEntry.ElementAt(1).Content.Allocation.FundingStream.PeriodType.StartDay.Should().Be(1);
            atomFeed.AtomEntry.ElementAt(1).Content.Allocation.FundingStream.PeriodType.StartMonth.Should().Be(8);
            atomFeed.AtomEntry.ElementAt(1).Content.Allocation.FundingStream.PeriodType.EndDay.Should().Be(31);
            atomFeed.AtomEntry.ElementAt(1).Content.Allocation.FundingStream.PeriodType.EndMonth.Should().Be(7);
            atomFeed.AtomEntry.ElementAt(1).Content.Allocation.Period.Name.Should().Be("fspi2");
            atomFeed.AtomEntry.ElementAt(1).Content.Allocation.Period.StartYear.Should().Be(2018);
            atomFeed.AtomEntry.ElementAt(1).Content.Allocation.Period.EndYear.Should().Be(2019);
            atomFeed.AtomEntry.ElementAt(1).Content.Allocation.Provider.Name.Should().Be("provider 2");
            atomFeed.AtomEntry.ElementAt(1).Content.Allocation.Provider.LegalName.Should().Be("legal 2");
            atomFeed.AtomEntry.ElementAt(1).Content.Allocation.Provider.Urn.Should().Be("02");
            atomFeed.AtomEntry.ElementAt(1).Content.Allocation.Provider.DfeEstablishmentNumber.Should().Be("dfe-2");
            atomFeed.AtomEntry.ElementAt(1).Content.Allocation.Provider.EstablishmentNumber.Should().Be("e-2");
            atomFeed.AtomEntry.ElementAt(1).Content.Allocation.Provider.LaCode.Should().Be("la-2");
            atomFeed.AtomEntry.ElementAt(1).Content.Allocation.Provider.LocalAuthority.Should().Be("authority");
            atomFeed.AtomEntry.ElementAt(1).Content.Allocation.Provider.Type.Should().Be("type 2");
            atomFeed.AtomEntry.ElementAt(1).Content.Allocation.Provider.SubType.Should().Be("sub type 2");
            atomFeed.AtomEntry.ElementAt(1).Content.Allocation.Provider.OpenDate.Should().NotBeNull();
            atomFeed.AtomEntry.ElementAt(1).Content.Allocation.Provider.CloseDate.Should().NotBeNull();
            atomFeed.AtomEntry.ElementAt(1).Content.Allocation.Provider.CrmAccountId.Should().Be("crm-2");
            atomFeed.AtomEntry.ElementAt(1).Content.Allocation.Provider.NavVendorNo.Should().Be("nv-2");
            atomFeed.AtomEntry.ElementAt(1).Content.Allocation.Provider.Status.Should().Be("Active");
            atomFeed.AtomEntry.ElementAt(1).Content.Allocation.AllocationLine.ShortName.Should().Be("short-al2");
            atomFeed.AtomEntry.ElementAt(1).Content.Allocation.AllocationLine.FundingRoute.Should().Be("LA");
            atomFeed.AtomEntry.ElementAt(1).Content.Allocation.AllocationLine.ContractRequired.Should().Be("Y");
            atomFeed.AtomEntry.ElementAt(2).Id.Should().Be("id-3");
            atomFeed.AtomEntry.ElementAt(2).Title.Should().Be("test title 3");
            atomFeed.AtomEntry.ElementAt(2).Summary.Should().Be("test summary 3");
            atomFeed.AtomEntry.ElementAt(2).Content.Allocation.FundingStream.Id.Should().Be("fs-3");
            atomFeed.AtomEntry.ElementAt(2).Content.Allocation.FundingStream.Name.Should().Be("fs 3");
            atomFeed.AtomEntry.ElementAt(2).Content.Allocation.Period.Id.Should().Be("fp-3");
            atomFeed.AtomEntry.ElementAt(2).Content.Allocation.Provider.UkPrn.Should().Be("3333");
            atomFeed.AtomEntry.ElementAt(2).Content.Allocation.Provider.Upin.Should().Be("0003");
            atomFeed.AtomEntry.ElementAt(2).Content.Allocation.AllocationLine.Id.Should().Be("al-3");
            atomFeed.AtomEntry.ElementAt(2).Content.Allocation.AllocationLine.Name.Should().Be("al 3");
            atomFeed.AtomEntry.ElementAt(2).Content.Allocation.AllocationVersionNumber.Should().Be(1);
            atomFeed.AtomEntry.ElementAt(2).Content.Allocation.AllocationStatus.Should().Be("Approved");
            atomFeed.AtomEntry.ElementAt(2).Content.Allocation.AllocationAmount.Should().Be(20);
            atomFeed.AtomEntry.ElementAt(2).Content.Allocation.ProfilePeriods.Length.Should().Be(0);
            atomFeed.AtomEntry.ElementAt(2).Content.Allocation.FundingStream.ShortName.Should().Be("fs-short-3");
            atomFeed.AtomEntry.ElementAt(2).Content.Allocation.FundingStream.PeriodType.Id.Should().Be("fspi-3");
            atomFeed.AtomEntry.ElementAt(2).Content.Allocation.FundingStream.PeriodType.StartDay.Should().Be(1);
            atomFeed.AtomEntry.ElementAt(2).Content.Allocation.FundingStream.PeriodType.StartMonth.Should().Be(8);
            atomFeed.AtomEntry.ElementAt(2).Content.Allocation.FundingStream.PeriodType.EndDay.Should().Be(31);
            atomFeed.AtomEntry.ElementAt(2).Content.Allocation.FundingStream.PeriodType.EndMonth.Should().Be(7);
            atomFeed.AtomEntry.ElementAt(2).Content.Allocation.Period.Name.Should().Be("fspi3");
            atomFeed.AtomEntry.ElementAt(2).Content.Allocation.Period.StartYear.Should().Be(2018);
            atomFeed.AtomEntry.ElementAt(2).Content.Allocation.Period.EndYear.Should().Be(2019);
            atomFeed.AtomEntry.ElementAt(2).Content.Allocation.Provider.Name.Should().Be("provider 3");
            atomFeed.AtomEntry.ElementAt(2).Content.Allocation.Provider.LegalName.Should().Be("legal 3");
            atomFeed.AtomEntry.ElementAt(2).Content.Allocation.Provider.Urn.Should().Be("03");
            atomFeed.AtomEntry.ElementAt(2).Content.Allocation.Provider.DfeEstablishmentNumber.Should().Be("dfe-3");
            atomFeed.AtomEntry.ElementAt(2).Content.Allocation.Provider.EstablishmentNumber.Should().Be("e-3");
            atomFeed.AtomEntry.ElementAt(2).Content.Allocation.Provider.LaCode.Should().Be("la-3");
            atomFeed.AtomEntry.ElementAt(2).Content.Allocation.Provider.LocalAuthority.Should().Be("authority");
            atomFeed.AtomEntry.ElementAt(2).Content.Allocation.Provider.Type.Should().Be("type 3");
            atomFeed.AtomEntry.ElementAt(2).Content.Allocation.Provider.SubType.Should().Be("sub type 3");
            atomFeed.AtomEntry.ElementAt(2).Content.Allocation.Provider.OpenDate.Should().NotBeNull();
            atomFeed.AtomEntry.ElementAt(2).Content.Allocation.Provider.CloseDate.Should().NotBeNull();
            atomFeed.AtomEntry.ElementAt(2).Content.Allocation.Provider.CrmAccountId.Should().Be("crm-3");
            atomFeed.AtomEntry.ElementAt(2).Content.Allocation.Provider.NavVendorNo.Should().Be("nv-3");
            atomFeed.AtomEntry.ElementAt(2).Content.Allocation.Provider.Status.Should().Be("Active");
            atomFeed.AtomEntry.ElementAt(2).Content.Allocation.AllocationLine.ShortName.Should().Be("short-al3");
            atomFeed.AtomEntry.ElementAt(2).Content.Allocation.AllocationLine.FundingRoute.Should().Be("LA");
            atomFeed.AtomEntry.ElementAt(2).Content.Allocation.AllocationLine.ContractRequired.Should().Be("Y");
        }

        [TestMethod]
        public async Task GetNotifications_GivenSearchFeedRetunsNoResultsWhenNoPageRefIsSpecified_EnsuresAtomLinksCorrect()
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
                .GetFeeds(Arg.Is(3), Arg.Is(2), Arg.Any<IEnumerable<string>>())
                .Returns(feeds);

            AllocationNotificationFeedsService service = CreateService(feedsSearchService);

            IHeaderDictionary headerDictionary = new HeaderDictionary();
            headerDictionary.Add("Accept", new StringValues("application/json"));

            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "allocationStatuses", new StringValues("Published,Approved") },
                { "pageSize", new StringValues("2") }
            });

            HttpRequest request = Substitute.For<HttpRequest>();
            request.Scheme.Returns("https");
            request.Path.Returns(new PathString("/api/v1/test"));
            request.Host.Returns(new HostString("wherever.naf:12345"));
            request.QueryString.Returns(new QueryString("?pageSize=2&allocationStatuses=Published,Approved"));
            request.Headers.Returns(headerDictionary);
            request.Query.Returns(queryStringValues);

            //Act
            IActionResult result = await service.GetNotifications(3, "Published,Approved", 2, request);

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
            atomFeed.Link.First(m => m.Rel == "self").Href.Should().Be("https://wherever.naf:12345/api/v1/notifications?allocationStatuses=Published,Approved&pageSize=2&pageRef=3");
            atomFeed.Link.First(m => m.Rel == "first").Href.Should().Be("https://wherever.naf:12345/api/v1/notifications?allocationStatuses=Published,Approved&pageSize=2&pageRef=1");
            atomFeed.Link.First(m => m.Rel == "last").Href.Should().Be("https://wherever.naf:12345/api/v1/notifications?allocationStatuses=Published,Approved&pageSize=2&pageRef=1");
            atomFeed.Link.First(m => m.Rel == "previous").Href.Should().Be("https://wherever.naf:12345/api/v1/notifications?allocationStatuses=Published,Approved&pageSize=2&pageRef=2");
            atomFeed.Link.First(m => m.Rel == "next").Href.Should().Be("https://wherever.naf:12345/api/v1/notifications?allocationStatuses=Published,Approved&pageSize=2&pageRef=3");
        }

        [TestMethod]
        public async Task GetNotifications_GivenSearchFeedRetunsNoResultsWhenNoQueryParameters_EnsuresAtomLinksCorrect()
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
                .GetFeeds(Arg.Is(3), Arg.Is(2), Arg.Any<IEnumerable<string>>())
                .Returns(feeds);

            AllocationNotificationFeedsService service = CreateService(feedsSearchService);

            IHeaderDictionary headerDictionary = new HeaderDictionary();
            headerDictionary.Add("Accept", new StringValues("application/json"));

            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "allocationStatuses", new StringValues("Published,Approved") },
                { "pageSize", new StringValues("2") }
            });

            HttpRequest request = Substitute.For<HttpRequest>();
            request.Scheme.Returns("https");
            request.Path.Returns(new PathString("/api/v1/test"));
            request.Host.Returns(new HostString("wherever.naf:12345"));
            request.Headers.Returns(headerDictionary);

            //Act
            IActionResult result = await service.GetNotifications(3, "Published,Approved", 2, request);

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
            atomFeed.Link.First(m => m.Rel == "self").Href.Should().Be("https://wherever.naf:12345/api/v1/notifications?pageRef=3");
            atomFeed.Link.First(m => m.Rel == "first").Href.Should().Be("https://wherever.naf:12345/api/v1/notifications?pageRef=1");
            atomFeed.Link.First(m => m.Rel == "last").Href.Should().Be("https://wherever.naf:12345/api/v1/notifications?pageRef=1");
            atomFeed.Link.First(m => m.Rel == "previous").Href.Should().Be("https://wherever.naf:12345/api/v1/notifications?pageRef=2");
            atomFeed.Link.First(m => m.Rel == "next").Href.Should().Be("https://wherever.naf:12345/api/v1/notifications?pageRef=3");
        }

        [TestMethod]
        public async Task GetNotifications_GivenSearchFeedRetunsNoResultsWhenNoQueryParametersAndWithToTotalCountOf17848_EnsuresAtomLinksCorrectAndLastIs36()
        {
            //Arrange
            SearchFeed<AllocationNotificationFeedIndex> feeds = new SearchFeed<AllocationNotificationFeedIndex>
            {
                PageRef = 1,
                Top = 500,
                TotalCount = 17848,
                Entries = CreateFeedIndexes()
            };

            IAllocationNotificationsFeedsSearchService feedsSearchService = CreateSearchService();
            feedsSearchService
                .GetFeeds(Arg.Is(1), Arg.Is(2), Arg.Any<IEnumerable<string>>())
                .Returns(feeds);

            AllocationNotificationFeedsService service = CreateService(feedsSearchService);

            IHeaderDictionary headerDictionary = new HeaderDictionary();
            headerDictionary.Add("Accept", new StringValues("application/json"));

            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "allocationStatuses", new StringValues("Published,Approved") }
            });

            HttpRequest request = Substitute.For<HttpRequest>();
            request.Scheme.Returns("https");
            request.Path.Returns(new PathString("/api/v1/test"));
            request.Host.Returns(new HostString("wherever.naf:12345"));
            request.Headers.Returns(headerDictionary);

            //Act
            IActionResult result = await service.GetNotifications(1, "Published,Approved", 2, request);

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
            atomFeed.Link.First(m => m.Rel == "self").Href.Should().Be("https://wherever.naf:12345/api/v1/notifications?pageRef=1");
            atomFeed.Link.First(m => m.Rel == "first").Href.Should().Be("https://wherever.naf:12345/api/v1/notifications?pageRef=1");
            atomFeed.Link.First(m => m.Rel == "last").Href.Should().Be("https://wherever.naf:12345/api/v1/notifications?pageRef=36");
            atomFeed.Link.First(m => m.Rel == "previous").Href.Should().Be("https://wherever.naf:12345/api/v1/notifications?pageRef=1");
            atomFeed.Link.First(m => m.Rel == "next").Href.Should().Be("https://wherever.naf:12345/api/v1/notifications?pageRef=2");
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
            IActionResult result = await service.GetNotifications(3, "Published,Approved", null, request);

            //Assert
            result
                .Should()
                .BeOfType<BadRequestResult>();
        }

        [TestMethod]
        public async Task GetNotifications_GivenMajorMinorFeatureToggleOn_ReturnsMajorMinorVersionNumbers()
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
                .GetFeeds(Arg.Is(3), Arg.Is(2), Arg.Any<IEnumerable<string>>())
                .Returns(feeds);

            IFeatureToggle features = CreateFeatureToggle();
            features
                .IsAllocationLineMajorMinorVersioningEnabled()
                .Returns(true);

            AllocationNotificationFeedsService service = CreateService(feedsSearchService, features);

            IHeaderDictionary headerDictionary = new HeaderDictionary
            {
                { "Accept", new StringValues("application/json") }
            };

            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "pageRef", new StringValues("3") },
                { "allocationStatuses", new StringValues("Published,Approved") },
                { "pageSize", new StringValues("2") }
            });

            HttpRequest request = Substitute.For<HttpRequest>();
            request.Scheme.Returns("https");
            request.Path.Returns(new PathString("/api/v1/test"));
            request.Host.Returns(new HostString("wherever.naf:12345"));
            request.QueryString.Returns(new QueryString("?pageRef=3&pageSize=2&allocationStatuses=Published,Approved"));
            request.Headers.Returns(headerDictionary);
            request.Query.Returns(queryStringValues);

            //Act
            IActionResult result = await service.GetNotifications(3, "Published,Approved", 2, request);

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
            atomFeed.AtomEntry.Count.Should().Be(3);
            atomFeed.AtomEntry.ElementAt(0).Content.Allocation.AllocationMajorVersion.Should().Be(1);
            atomFeed.AtomEntry.ElementAt(0).Content.Allocation.AllocationMinorVersion.Should().Be(0);
            atomFeed.AtomEntry.ElementAt(1).Content.Allocation.AllocationMajorVersion.Should().Be(2);
            atomFeed.AtomEntry.ElementAt(1).Content.Allocation.AllocationMinorVersion.Should().Be(0);
            atomFeed.AtomEntry.ElementAt(2).Content.Allocation.AllocationMajorVersion.Should().Be(0);
            atomFeed.AtomEntry.ElementAt(2).Content.Allocation.AllocationMinorVersion.Should().Be(3);
        }

        [TestMethod]
        public async Task GetNotifications_GivenMajorMinorFeatureToggleOff_ReturnsMajorMinorVersionNumbersAsZero()
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
                .GetFeeds(Arg.Is(3), Arg.Is(2), Arg.Any<IEnumerable<string>>())
                .Returns(feeds);

            IFeatureToggle features = CreateFeatureToggle();
            features
                .IsAllocationLineMajorMinorVersioningEnabled()
                .Returns(false);

            AllocationNotificationFeedsService service = CreateService(feedsSearchService, features);

            IHeaderDictionary headerDictionary = new HeaderDictionary
            {
                { "Accept", new StringValues("application/json") }
            };

            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "pageRef", new StringValues("3") },
                { "allocationStatuses", new StringValues("Published,Approved") },
                { "pageSize", new StringValues("2") }
            });

            HttpRequest request = Substitute.For<HttpRequest>();
            request.Scheme.Returns("https");
            request.Path.Returns(new PathString("/api/v1/test"));
            request.Host.Returns(new HostString("wherever.naf:12345"));
            request.QueryString.Returns(new QueryString("?pageRef=3&pageSize=2&allocationStatuses=Published,Approved"));
            request.Headers.Returns(headerDictionary);
            request.Query.Returns(queryStringValues);

            //Act
            IActionResult result = await service.GetNotifications(3, "Published,Approved", 2, request);

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
            atomFeed.AtomEntry.Count.Should().Be(3);
            atomFeed.AtomEntry.ElementAt(0).Content.Allocation.AllocationMajorVersion.Should().Be(0);
            atomFeed.AtomEntry.ElementAt(0).Content.Allocation.AllocationMinorVersion.Should().Be(0);
            atomFeed.AtomEntry.ElementAt(1).Content.Allocation.AllocationMajorVersion.Should().Be(0);
            atomFeed.AtomEntry.ElementAt(1).Content.Allocation.AllocationMinorVersion.Should().Be(0);
            atomFeed.AtomEntry.ElementAt(2).Content.Allocation.AllocationMajorVersion.Should().Be(0);
            atomFeed.AtomEntry.ElementAt(2).Content.Allocation.AllocationMinorVersion.Should().Be(0);
        }

        private static AllocationNotificationFeedsService CreateService(IAllocationNotificationsFeedsSearchService searchService = null, IFeatureToggle featureToggle = null)
        {
            return new AllocationNotificationFeedsService(searchService ?? CreateSearchService(), featureToggle ?? CreateFeatureToggle());
        }

        private static IFeatureToggle CreateFeatureToggle()
        {
            return Substitute.For<IFeatureToggle>();
        }

        private static IAllocationNotificationsFeedsSearchService CreateSearchService()
        {
            return Substitute.For<IAllocationNotificationsFeedsSearchService>();
        }

        private static IEnumerable<AllocationNotificationFeedIndex> CreateFeedIndexes()
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
                         FundingPeriodEndYear = 2019,
                         FundingPeriodId = "fp-1",
                         FundingPeriodStartYear = 2018,
                         FundingStreamId = "fs-1",
                         FundingStreamName = "fs 1",
                         Id = "id-1",
                         ProviderId = "1111",
                         ProviderUkPrn = "1111",
                         ProviderUpin = "0001",
                         Summary = "test summary 1",
                         Title = "test title 1",
                         ProviderProfiling = "[]",
                         AllocationLineContractRequired = true,
                         AllocationLineFundingRoute = "LA",
                         AllocationLineShortName = "short-al1",
                         Authority = "authority",
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
                         LaCode = "la-1",
                         NavVendorNo = "nv-1",
                         ProviderClosedDate = DateTimeOffset.Now,
                         ProviderLegalName = "legal 1",
                         ProviderName = "provider 1",
                         ProviderOpenDate = DateTimeOffset.Now,
                         ProviderStatus = "Active",
                         ProviderType = "type 1",
                         SubProviderType = "sub type 1",
                         ProviderUrn = "01",
                         MajorVersion = 1,
                         MinorVersion = 0
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
                         FundingPeriodEndYear = 2019,
                         FundingPeriodId = "fp-2",
                         FundingPeriodStartYear = 2018,
                         FundingStreamId = "fs-2",
                         FundingStreamName = "fs 2",
                         Id = "id-2",
                         ProviderId = "2222",
                         ProviderUkPrn = "2222",
                         ProviderUpin = "0002",
                         Summary = "test summary 2",
                         Title = "test title 2",
                         ProviderProfiling = "[{\"period\":\"Oct\",\"occurrence\":1,\"periodYear\":2017,\"periodType\":\"CalendarMonth\",\"periodValue\":5.5,\"distributionPeriod\":\"2017-2018\"},{\"period\":\"Apr\",\"occurrence\":1,\"periodYear\":2018,\"periodType\":\"CalendarMonth\",\"periodValue\":5.5,\"distributionPeriod\":\"2017-2018\"}]",
                         AllocationLineContractRequired = true,
                         AllocationLineFundingRoute = "LA",
                         AllocationLineShortName = "short-al2",
                         Authority = "authority",
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
                         LaCode = "la-2",
                         NavVendorNo = "nv-2",
                         ProviderClosedDate = DateTimeOffset.Now,
                         ProviderLegalName = "legal 2",
                         ProviderName = "provider 2",
                         ProviderOpenDate = DateTimeOffset.Now,
                         ProviderStatus = "Active",
                         ProviderType = "type 2",
                         SubProviderType = "sub type 2",
                         ProviderUrn = "02",
                         MajorVersion = 2,
                         MinorVersion = 0
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
                         FundingPeriodEndYear = 2019,
                         FundingPeriodId = "fp-3",
                         FundingPeriodStartYear = 2018,
                         FundingStreamId = "fs-3",
                         FundingStreamName = "fs 3",
                         Id = "id-3",
                         ProviderId = "3333",
                         ProviderUkPrn = "3333",
                         ProviderUpin = "0003",
                         Summary = "test summary 3",
                         Title = "test title 3",
                         ProviderProfiling = "[]",
                         AllocationLineContractRequired = true,
                         AllocationLineFundingRoute = "LA",
                         AllocationLineShortName = "short-al3",
                         Authority = "authority",
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
                         LaCode = "la-3",
                         NavVendorNo = "nv-3",
                         ProviderClosedDate = DateTimeOffset.Now,
                         ProviderLegalName = "legal 3",
                         ProviderName = "provider 3",
                         ProviderOpenDate = DateTimeOffset.Now,
                         ProviderStatus = "Active",
                         ProviderType = "type 3",
                         SubProviderType = "sub type 3",
                         ProviderUrn = "03",
                         MajorVersion = 0,
                         MinorVersion = 3
                    }
                };
        }
    }
}
