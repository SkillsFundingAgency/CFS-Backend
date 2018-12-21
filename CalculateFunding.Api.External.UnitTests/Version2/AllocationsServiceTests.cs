using System;
using System.Text;
using System.Threading.Tasks;
using CalculateFunding.Api.External.V2.Models;
using CalculateFunding.Api.External.V2.Services;
using CalculateFunding.Common.FeatureToggles;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Results;
using CalculateFunding.Services.Results.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using NSubstitute;

namespace CalculateFunding.Api.External.UnitTests.Version2
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

            IPublishedResultsService resultsService = CreateResultsService();
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

            IPublishedResultsService resultsService = CreateResultsService();
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

            IPublishedResultsService resultsService = CreateResultsService();
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

            string id = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{publishedProviderResult.SpecificationId}{publishedProviderResult.ProviderId}{publishedProviderResult.FundingStreamResult.AllocationLineResult.AllocationLine.Id}"));

            AllocationModel allocationModel = JsonConvert.DeserializeObject<AllocationModel>(contentResult.Content);

            allocationModel
                .Should()
                .NotBeNull();

            allocationModel.AllocationResultId.Should().Be(id);
            allocationModel.AllocationVersionNumber.Should().Be(1);
            allocationModel.AllocationStatus.Should().Be("Published");
            allocationModel.AllocationAmount.Should().Be(50);
            allocationModel.FundingStream.Id.Should().Be("fs-1");
            allocationModel.FundingStream.Name.Should().Be("funding stream 1");
            allocationModel.Period.Id.Should().Be("Ay12345");
            allocationModel.Provider.UkPrn.Should().Be("1111");
            allocationModel.Provider.Upin.Should().Be("2222");
            allocationModel.Provider.OpenDate.Should().NotBeNull();
            allocationModel.AllocationLine.Id.Should().Be("AAAAA");
            allocationModel.AllocationLine.Name.Should().Be("test allocation line 1");
            allocationModel.ProfilePeriods.Length.Should().Be(1);
        }

        [TestMethod]
        public async Task GetAllocationAndHistoryByAllocationResultId_GivenNullResultFound_ReturnsNotFound()
        {
            //Arrange
            string allocationResultId = "12345";

            HttpRequest request = Substitute.For<HttpRequest>();

            IPublishedResultsService resultsService = CreateResultsService();
            resultsService
                .GetPublishedProviderResultWithHistoryByAllocationResultId(Arg.Is(allocationResultId))
                .Returns((PublishedProviderResultWithHistory)null);

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

            PublishedProviderResultWithHistory publishedProviderResult = CreatePublishedProviderResultWithHistory();

            IPublishedResultsService resultsService = CreateResultsService();
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

            PublishedProviderResultWithHistory publishedProviderResult = CreatePublishedProviderResultWithHistory();


            IPublishedResultsService resultsService = CreateResultsService();
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

            string id = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{publishedProviderResult.PublishedProviderResult.SpecificationId}{publishedProviderResult.PublishedProviderResult.ProviderId}{publishedProviderResult.PublishedProviderResult.FundingStreamResult.AllocationLineResult.AllocationLine.Id}"));

            allocationModel
                .Should()
                .NotBeNull();

            allocationModel.AllocationResultId.Should().Be(id);
            allocationModel.AllocationVersionNumber.Should().Be(1);
            allocationModel.AllocationStatus.Should().Be("Published");
            allocationModel.AllocationAmount.Should().Be(50);
            allocationModel.FundingStream.Id.Should().Be("fs-1");
            allocationModel.FundingStream.Name.Should().Be("funding stream 1");
            allocationModel.Period.Id.Should().Be("Ay12345");
            allocationModel.Provider.UkPrn.Should().Be("1111");
            allocationModel.Provider.Upin.Should().Be("2222");
            allocationModel.Provider.OpenDate.Should().NotBeNull();
            allocationModel.AllocationLine.Id.Should().Be("AAAAA");
            allocationModel.AllocationLine.Name.Should().Be("test allocation line 1");
            allocationModel.ProfilePeriods.Length.Should().Be(1);
            allocationModel.History.Length.Should().Be(2);
            allocationModel.History[0].AllocationVersionNumber.Should().Be(2);
            allocationModel.History[0].AllocationAmount.Should().Be(40);
            allocationModel.History[0].Status.Should().Be("Approved");
            allocationModel.History[1].AllocationVersionNumber.Should().Be(1);
            allocationModel.History[1].AllocationAmount.Should().Be(50);
            allocationModel.History[1].Status.Should().Be("Held");
        }

        [TestMethod]
        public async Task GetAllocationByAllocationResultId_GivenMajorMinorFeatureToggleOn_ReturnsMajorMinorVersions()
        {
            //Arrange
            string allocationResultId = "12345";

            IHeaderDictionary headerDictionary = new HeaderDictionary
            {
                { "Accept", new StringValues("application/json") }
            };

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Headers
                .Returns(headerDictionary);

            PublishedProviderResult publishedProviderResult = CreatePublishedProviderResult();

            IPublishedResultsService resultsService = CreateResultsService();
            resultsService
                .GetPublishedProviderResultByAllocationResultId(Arg.Is(allocationResultId))
                .Returns(publishedProviderResult);

            IFeatureToggle features = CreateFeatureToggle();
            features
                .IsAllocationLineMajorMinorVersioningEnabled()
                .Returns(true);

            AllocationsService service = CreateService(resultsService, features);

            //Act
            IActionResult result = await service.GetAllocationByAllocationResultId(allocationResultId, null, request);

            //Assert
            result
                .Should()
                .BeOfType<ContentResult>();

            ContentResult contentResult = result as ContentResult;

            string id = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{publishedProviderResult.SpecificationId}{publishedProviderResult.ProviderId}{publishedProviderResult.FundingStreamResult.AllocationLineResult.AllocationLine.Id}"));

            AllocationModel allocationModel = JsonConvert.DeserializeObject<AllocationModel>(contentResult.Content);

            allocationModel
                .Should()
                .NotBeNull();

            allocationModel.AllocationMajorVersion.Should().Be(1);
            allocationModel.AllocationMinorVersion.Should().Be(1);
        }

        [TestMethod]
        public async Task GetAllocationByAllocationResultId_GivenMajorMinorFeatureToggleOff_ReturnsMajorMinorVersionsAsZero()
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

            IPublishedResultsService resultsService = CreateResultsService();
            resultsService
                .GetPublishedProviderResultByAllocationResultId(Arg.Is(allocationResultId))
                .Returns(publishedProviderResult);

            IFeatureToggle features = CreateFeatureToggle();
            features
                .IsAllocationLineMajorMinorVersioningEnabled()
                .Returns(false);

            AllocationsService service = CreateService(resultsService, features);

            //Act
            IActionResult result = await service.GetAllocationByAllocationResultId(allocationResultId, null, request);

            //Assert
            result
                .Should()
                .BeOfType<ContentResult>();

            ContentResult contentResult = result as ContentResult;

            string id = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{publishedProviderResult.SpecificationId}{publishedProviderResult.ProviderId}{publishedProviderResult.FundingStreamResult.AllocationLineResult.AllocationLine.Id}"));

            AllocationModel allocationModel = JsonConvert.DeserializeObject<AllocationModel>(contentResult.Content);

            allocationModel
                .Should()
                .NotBeNull();

            allocationModel.AllocationMajorVersion.Should().Be(0);
            allocationModel.AllocationMinorVersion.Should().Be(0);
        }

        private static AllocationsService CreateService(IPublishedResultsService resultsService = null, IFeatureToggle featureToggle = null)
        {
            return new AllocationsService(resultsService ?? CreateResultsService(), featureToggle ?? CreateFeatureToggle());
        }

        private static IFeatureToggle CreateFeatureToggle()
        {
            return Substitute.For<IFeatureToggle>();
        }

        private static IPublishedResultsService CreateResultsService()
        {
            return Substitute.For<IPublishedResultsService>();
        }

        private static PublishedProviderResult CreatePublishedProviderResult()
        {
            return new PublishedProviderResult
            {
                Title = "test title 1",

                SpecificationId = "spec-1",
                FundingStreamResult = new PublishedFundingStreamResult
                {
                    FundingStream = new Models.Specs.FundingStream
                    {
                        Id = "fs-1",
                        Name = "funding stream 1"
                    },
                    AllocationLineResult = new PublishedAllocationLineResult
                    {
                        AllocationLine = new Models.Specs.AllocationLine
                        {
                            Id = "AAAAA",
                            Name = "test allocation line 1"
                        },
                        Current = new PublishedAllocationLineResultVersion
                        {
                            Status = AllocationLineStatus.Published,
                            Value = 50,
                            Version = 1,
                            Major = 1,
                            Minor = 1,
                            Date = DateTimeOffset.Now,
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
                        }
                    }
                },
                FundingPeriod = new Models.Specs.Period
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

        private static PublishedProviderResultWithHistory CreatePublishedProviderResultWithHistory()
        {
            return new PublishedProviderResultWithHistory
            {
                PublishedProviderResult = CreatePublishedProviderResult(),
                History = new[]
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
                 }
            };
        }
    }
}
