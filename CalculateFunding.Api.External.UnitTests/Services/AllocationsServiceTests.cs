using CalculateFunding.Api.External.V1.Models;
using CalculateFunding.Api.External.V1.Services;
using CalculateFunding.Models;
using CalculateFunding.Models.Results;
using CalculateFunding.Models.Specs;
using CalculateFunding.Services.Results.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Api.External.UnitTests.Services
{
    [TestClass]
    public class AllocationsServiceTests
    {
        [TestMethod]
        public async Task GetAllocationByAllocationResultId_GivenInvalidVersion_ReturnsBadRequest()
        {
            //Arrange
            int version = 0;

            string allocationResultId = "12345";

            AllocationsService service = CreateService();

            HttpRequest request = Substitute.For<HttpRequest>();

            //Act
            IActionResult result = await service.GetAllocationByAllocationResultId(allocationResultId, version, request);

            //Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>()
                .Which
                .Value
                .Should()
                .Be("Invalid version supplied");
        }

        [TestMethod]
        public async Task GetAllocationByAllocationResultId_GivenNullResultFound_ReturnsNotFound()
        {
            //Arrange
            string allocationResultId = "12345";

            HttpRequest request = Substitute.For<HttpRequest>();

            IResultsService resultsService = CreateResultsService();
            resultsService
                .GetPublishedProviderResultByAllocationResultId(Arg.Is(allocationResultId))
                .Returns((PublishedProviderResult)null);

            AllocationsService service = CreateService(resultsService);

            //Act
            IActionResult result = await service.GetAllocationByAllocationResultId(allocationResultId, null, request);

            //Assert
            result
                .Should()
                .BeOfType<NotFoundResult>();
        }

        [TestMethod]
        public async Task GetAllocationByAllocationResultId_GivenResultFoundButNoHeaders_ReturnsBadRequest()
        {
            //Arrange
            string allocationResultId = "12345";

            IHeaderDictionary headerDictionary = new HeaderDictionary();
            
            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Headers
                .Returns(headerDictionary);

            PublishedProviderResult publishedProviderResult = CreatePublishedProviderResult();

            IResultsService resultsService = CreateResultsService();
            resultsService
                .GetPublishedProviderResultByAllocationResultId(Arg.Is(allocationResultId))
                .Returns(publishedProviderResult);

            AllocationsService service = CreateService(resultsService);

            //Act
            IActionResult result = await service.GetAllocationByAllocationResultId(allocationResultId, null, request);

            //Assert
            result
                .Should()
                .BeOfType<BadRequestResult>();
        }

        [TestMethod]
        public async Task GetAllocationByAllocationResultId_GivenResultFound_ReturnsContentResult()
        {
            //Arrange
            string allocationResultId = "12345";

            IHeaderDictionary headerDictionary = new HeaderDictionary();
            headerDictionary.Add("Accept", new StringValues("application/json"));

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Headers
                .Returns(headerDictionary);

            PublishedProviderResult publishedProviderResult = CreatePublishedProviderResult();

            IResultsService resultsService = CreateResultsService();
            resultsService
                .GetPublishedProviderResultByAllocationResultId(Arg.Is(allocationResultId))
                .Returns(publishedProviderResult);

            AllocationsService service = CreateService(resultsService);

            //Act
            IActionResult result = await service.GetAllocationByAllocationResultId(allocationResultId, null, request);

            //Assert
            result
                .Should()
                .BeOfType<ContentResult>();

            ContentResult contentResult = result as ContentResult;

            AllocationModel allocationModel = JsonConvert.DeserializeObject<AllocationModel>(contentResult.Content);

            allocationModel
                .Should()
                .NotBeNull();

            allocationModel.AllocationResultId.Should().Be("1234567");
            allocationModel.AllocationVersionNumber.Should().Be(1);
            allocationModel.AllocationStatus.Should().Be("Published");
            allocationModel.AllocationAmount.Should().Be(50);
            allocationModel.FundingStream.FundingStreamCode.Should().Be("fs-1");
            allocationModel.FundingStream.FundingStreamName.Should().Be("funding stream 1");
            allocationModel.Period.PeriodType.Should().Be("fp-1");
            allocationModel.Period.PeriodId.Should().Be("Ay12345");
            allocationModel.Provider.Ukprn.Should().Be("1111");
            allocationModel.Provider.Upin.Should().Be("2222");
            allocationModel.Provider.ProviderOpenDate.Should().NotBeNull();
            allocationModel.AllocationLine.AllocationLineCode.Should().Be("AAAAA");
            allocationModel.AllocationLine.AllocationLineName.Should().Be("test allocation line 1");
            allocationModel.ProfilePeriods.Length.Should().Be(1);
        }

        [TestMethod]
        public async Task GetAllocationAndHistoryByAllocationResultId_GivenNullResultFound_ReturnsNotFound()
        {
            //Arrange
            string allocationResultId = "12345";

            HttpRequest request = Substitute.For<HttpRequest>();

            IResultsService resultsService = CreateResultsService();
            resultsService
                .GetPublishedProviderResultWithHistoryByAllocationResultId(Arg.Is(allocationResultId))
                .Returns((PublishedProviderResult)null);

            AllocationsService service = CreateService(resultsService);

            //Act
            IActionResult result = await service.GetAllocationAndHistoryByAllocationResultId(allocationResultId, request);

            //Assert
            result
                .Should()
                .BeOfType<NotFoundResult>();
        }

        [TestMethod]
        public async Task GetAllocationAndHistoryByAllocationResultId_GivenResultFoundButNoHeaders_ReturnsBadRequest()
        {
            //Arrange
            string allocationResultId = "12345";

            IHeaderDictionary headerDictionary = new HeaderDictionary();

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Headers
                .Returns(headerDictionary);

            PublishedProviderResult publishedProviderResult = CreatePublishedProviderResult();

            IResultsService resultsService = CreateResultsService();
            resultsService
                .GetPublishedProviderResultWithHistoryByAllocationResultId(Arg.Is(allocationResultId))
                .Returns(publishedProviderResult);

            AllocationsService service = CreateService(resultsService);

            //Act
            IActionResult result = await service.GetAllocationAndHistoryByAllocationResultId(allocationResultId, request);

            //Assert
            result
                .Should()
                .BeOfType<BadRequestResult>();
        }

        [TestMethod]
        public async Task GetAllocationAndHistoryByAllocationResultId_GivenResultFound_ReturnsContentResult()
        {
            //Arrange
            string allocationResultId = "12345";

            IHeaderDictionary headerDictionary = new HeaderDictionary();
            headerDictionary.Add("Accept", new StringValues("application/json"));

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Headers
                .Returns(headerDictionary);

            PublishedProviderResult publishedProviderResult = CreatePublishedProviderResult();
            publishedProviderResult.FundingStreamResult.AllocationLineResult.History = new List<PublishedAllocationLineResultVersion>
            {
                new PublishedAllocationLineResultVersion
                {
                    Value = 50,
                    Version = 1,
                    Status = AllocationLineStatus.Held,
                    Author = new Reference
                    {
                        Name = "Joe Bloggs"
                    },
                    Commment = "Wahey",
                    Date = DateTimeOffset.Now.AddDays(-2)
                },
                 new PublishedAllocationLineResultVersion
                {
                    Value = 40,
                    Version = 2,
                    Status = AllocationLineStatus.Approved,
                    Author = new Reference
                    {
                        Name = "Joe Bloggs"
                    },
                    Commment = "Wahey",
                    Date = DateTimeOffset.Now.AddDays(-1)
                }
            };

            IResultsService resultsService = CreateResultsService();
            resultsService
                .GetPublishedProviderResultWithHistoryByAllocationResultId(Arg.Is(allocationResultId))
                .Returns(publishedProviderResult);

            AllocationsService service = CreateService(resultsService);

            //Act
            IActionResult result = await service.GetAllocationAndHistoryByAllocationResultId(allocationResultId, request);

            //Assert
            result
                .Should()
                .BeOfType<ContentResult>();

            ContentResult contentResult = result as ContentResult;

            AllocationWithHistoryModel allocationModel = JsonConvert.DeserializeObject<AllocationWithHistoryModel>(contentResult.Content);

            allocationModel
                .Should()
                .NotBeNull();

            allocationModel.AllocationResultId.Should().Be("1234567");
            allocationModel.AllocationVersionNumber.Should().Be(1);
            allocationModel.AllocationStatus.Should().Be("Published");
            allocationModel.AllocationAmount.Should().Be(50);
            allocationModel.FundingStream.FundingStreamCode.Should().Be("fs-1");
            allocationModel.FundingStream.FundingStreamName.Should().Be("funding stream 1");
            allocationModel.Period.PeriodType.Should().Be("fp-1");
            allocationModel.Period.PeriodId.Should().Be("Ay12345");
            allocationModel.Provider.Ukprn.Should().Be("1111");
            allocationModel.Provider.Upin.Should().Be("2222");
            allocationModel.Provider.ProviderOpenDate.Should().NotBeNull();
            allocationModel.AllocationLine.AllocationLineCode.Should().Be("AAAAA");
            allocationModel.AllocationLine.AllocationLineName.Should().Be("test allocation line 1");
            allocationModel.ProfilePeriods.Length.Should().Be(1);
            allocationModel.History.Length.Should().Be(2);
            allocationModel.History[0].AllocationVersionNumber.Should().Be(2);
            allocationModel.History[0].AllocationAmount.Should().Be(40);
            allocationModel.History[0].Status.Should().Be("Approved");
            allocationModel.History[1].AllocationVersionNumber.Should().Be(1);
            allocationModel.History[1].AllocationAmount.Should().Be(50);
            allocationModel.History[1].Status.Should().Be("Held");
        }

        static AllocationsService CreateService(IResultsService resultsService = null)
        {
            return new AllocationsService(resultsService ?? CreateResultsService());
        }

        static IResultsService CreateResultsService()
        {
            return Substitute.For<IResultsService>();
        }

        static PublishedProviderResult CreatePublishedProviderResult()
        {
            return new PublishedProviderResult
            {
                Title = "test title 1",
                Summary = "test summary 1",
                Id = "1234567",
                Provider = new ProviderSummary
                {
                    URN = "12345",
                    UKPRN = "1111",
                    UPIN = "2222",
                    EstablishmentNumber = "es123",
                    Authority = "London",
                    ProviderType = "test type",
                    ProviderSubType = "test sub type",
                    DateOpened = DateTimeOffset.Now,
                    ProviderProfileIdType = "UKPRN",
                    LACode = "77777",
                    Id = "1111",
                    Name = "test provider name 1"
                },
                SpecificationId = "spec-1",
                FundingStreamResult = new PublishedFundingStreamResult
                {
                    FundingStream = new Reference
                    {
                        Id = "fs-1",
                        Name = "funding stream 1"
                    },
                    AllocationLineResult = new PublishedAllocationLineResult
                    {
                        AllocationLine = new Reference
                        {
                            Id = "AAAAA",
                            Name = "test allocation line 1"
                        },
                        Current = new PublishedAllocationLineResultVersion
                        {
                            Status = AllocationLineStatus.Published,
                            Value = 50,
                            Version = 1,
                            Date = DateTimeOffset.Now
                        }
                    }
                },
                FundingPeriod = new FundingPeriod
                {
                    Id = "Ay12345",
                    Name = "fp-1"
                },
                ProfilingPeriods = new[]
                {
                    new ProfilingPeriod()
                }
            };
        }
    }
}
