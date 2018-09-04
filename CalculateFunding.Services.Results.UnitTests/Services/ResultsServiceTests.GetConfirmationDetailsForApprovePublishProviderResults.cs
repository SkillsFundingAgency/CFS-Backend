using CalculateFunding.Models.Results;
using CalculateFunding.Services.Results.Interfaces;
using CalculateFunding.Services.Results.ResultModels;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using NSubstitute;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Results.Services
{
    public partial class ResultsServiceTests
    {
        [TestMethod]
        public async Task GetConfirmationDetailsForApprovePublishProviderResults_GivenNoSpecificationIdProvided_ReturnsBadRequest()
        {
            //Arrange
            HttpRequest request = Substitute.For<HttpRequest>();

            ILogger logger = CreateLogger();

            ResultsService resultsService = CreateResultsService(logger);

            //Act
            IActionResult actionResult = await resultsService.GetConfirmationDetailsForApprovePublishProviderResults(request);

            //Arrange
            actionResult
                .Should()
                .BeOfType<BadRequestObjectResult>()
                .Which
                .Value
                .Should()
                .Be("Null or empty specification Id provided");

            logger
                .Received(1)
                .Error("No specification Id was provided to GetConfirmationDetailsForApprovePublishProviderResults");
        }

        [TestMethod]
        public async Task GetConfirmationDetailsForApprovePublishProviderResults_GivenNoUpdateModelProvided_ReturnsBadRequest()
        {
            //arrange
            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "specificationId", new StringValues(specificationId) },
            });

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);

            ILogger logger = CreateLogger();

            ResultsService resultsService = CreateResultsService(logger);

            //Act
            IActionResult actionResult = await resultsService.GetConfirmationDetailsForApprovePublishProviderResults(request);

            //Arrange
            actionResult
                .Should()
                .BeOfType<BadRequestObjectResult>()
                .Which
                .Value
                .Should()
                .Be("Null filterCriteria was provided");

            logger
                .Received(1)
                .Error("Null filterCriteria was provided to GetConfirmationDetailsForApprovePublishProviderResults");
        }

        [TestMethod]
        public async Task GetConfirmationDetailsForApprovePublishProviderResults_GivenUpdateModelWithNoProviders_ReturnsBadRequest()
        {
            //arrange
            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "specificationId", new StringValues(specificationId) },
            });

            UpdatePublishedAllocationLineResultStatusModel model = new UpdatePublishedAllocationLineResultStatusModel();
            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);
            request
                .Body
                .Returns(stream);

            ILogger logger = CreateLogger();

            ResultsService resultsService = CreateResultsService(logger);

            //Act
            IActionResult actionResult = await resultsService.GetConfirmationDetailsForApprovePublishProviderResults(request);

            //Arrange
            actionResult
                .Should()
                .BeOfType<BadRequestObjectResult>()
                .Which
                .Value
                .Should()
                .Be("Null or empty providers was provided");

            logger
                .Received(1)
                .Error("Null or empty providers was provided to GetConfirmationDetailsForApprovePublishProviderResults");
        }

        [TestMethod]
        public async Task GetConfirmationDetailsForApprovePublishProviderResults_GivenNoPublishedResultsReturns_ReturnsZeroResults()
        {
            //arrange
            IEnumerable<UpdatePublishedAllocationLineResultStatusProviderModel> Providers = new[]
            {
                new UpdatePublishedAllocationLineResultStatusProviderModel
                {
                    ProviderId = "1234"
                }
            };

            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "specificationId", new StringValues(specificationId) },
            });

            UpdatePublishedAllocationLineResultStatusModel model = new UpdatePublishedAllocationLineResultStatusModel
            {
                Providers = Providers
            };

            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);
            request
                .Body
                .Returns(stream);

            IPublishedProviderResultsRepository resultsProviderRepository = CreatePublishedProviderResultsRepository();
            resultsProviderRepository
                .GetPublishedProviderResultsForSpecificationId(Arg.Is(specificationId))
                .Returns(Enumerable.Empty<PublishedProviderResult>());

            ResultsService resultsService = CreateResultsService(publishedProviderResultsRepository: resultsProviderRepository);

            //Act
            IActionResult actionResult = await resultsService.GetConfirmationDetailsForApprovePublishProviderResults(request);

            //Arrange
            OkObjectResult okResult = actionResult
                .Should()
                .BeOfType<OkObjectResult>()
                .Subject;

            ConfirmPublishApproveModel x = okResult.Value.Should().BeAssignableTo<ConfirmPublishApproveModel>().Subject;
            x.NumberOfProviders.Should().Be(0, nameof(x.NumberOfProviders));
            x.LocalAuthorities.Should().HaveCount(0, nameof(x.LocalAuthorities));
            x.ProviderTypes.Should().HaveCount(0, nameof(x.ProviderTypes));
            x.FundingPeriod.Should().BeNullOrEmpty("FundingPeriod should be blank");
            x.FundingStreams.Should().BeEmpty(nameof(x.FundingStreams));
            x.TotalFundingApproved.Should().Be(0, "TotalFundingApproved should be zero as there are no results");
        }

        [TestMethod]
        public async Task GetConfirmationDetailsForApprovePublishProviderResults_GivenPublishedResultsReturnsSingluar_ReturnsOkAndResult()
        {
            // Arrange
            IEnumerable<UpdatePublishedAllocationLineResultStatusProviderModel> Providers = new[]
            {
                new UpdatePublishedAllocationLineResultStatusProviderModel { ProviderId = "1" }
            };

            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "specificationId", new StringValues(specificationId) },
            });

            UpdatePublishedAllocationLineResultStatusModel model = new UpdatePublishedAllocationLineResultStatusModel
            {
                Status = AllocationLineStatus.Held,
                Providers = Providers
            };

            IEnumerable<PublishedProviderResult> publishedProviderResults = new List<PublishedProviderResult>
            {
                new PublishedProviderResult
                {
                    Title = "Test 1",
                    Summary = "testing",
                    FundingPeriod = new Models.Specs.Period { Name = "Period1" },
                    FundingStreamResult = new PublishedFundingStreamResult
                    {
                        FundingStream = new Models.Reference { Name = "Stream1" },
                        AllocationLineResult = new PublishedAllocationLineResult
                        {
                            AllocationLine = new Models.Reference { Name = "AllocLine1" },
                            Current = new PublishedAllocationLineResultVersion { Value = 12, Provider = new ProviderSummary { Id = "1", Authority = "Auth1", ProviderType = "PType1" } }
                        }
                    }
                }
            };

            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);
            request
                .Body
                .Returns(stream);

            IPublishedProviderResultsRepository resultsProviderRepository = CreatePublishedProviderResultsRepository();
            resultsProviderRepository
                .GetPublishedProviderResultsForSpecificationAndStatus(Arg.Is(specificationId), Arg.Any<UpdatePublishedAllocationLineResultStatusModel>())
                .Returns(publishedProviderResults);

            ResultsService resultsService = CreateResultsService(publishedProviderResultsRepository: resultsProviderRepository);

            // Act
            IActionResult actionResult = await resultsService.GetConfirmationDetailsForApprovePublishProviderResults(request);

            // Assert
            AssertConfirmPublishApproveModel(actionResult, publishedProviderResults.Count(), 1, 1, "Period1", 1, 12);
        }

        [TestMethod]
        public async Task GetConfirmationDetailsForApprovePublishProviderResults_GivenPublishedResultsReturnsMultiple_ReturnsOkAndAggregatesResults()
        {
            // Arrange
            IEnumerable<UpdatePublishedAllocationLineResultStatusProviderModel> Providers = new[]
            {
                new UpdatePublishedAllocationLineResultStatusProviderModel
                {
                    ProviderId = "1"
                }
            };

            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "specificationId", new StringValues(specificationId) },
            });

            UpdatePublishedAllocationLineResultStatusModel model = new UpdatePublishedAllocationLineResultStatusModel
            {
                Status = AllocationLineStatus.Held,
                Providers = Providers
            };

            IEnumerable<PublishedProviderResult> publishedProviderResults = new List<PublishedProviderResult>
            {
                new PublishedProviderResult
                {
                    Title = "Test 1",
                    Summary = "testing",
                    FundingPeriod = new Models.Specs.Period { Name = "Period1" },
                    FundingStreamResult = new PublishedFundingStreamResult
                    {
                        FundingStream = new Models.Reference { Name = "Stream1" },
                        AllocationLineResult = new PublishedAllocationLineResult
                        {
                            AllocationLine = new Models.Reference { Name = "AllocLine1" },
                            Current = new PublishedAllocationLineResultVersion { Value = 12, Provider = new ProviderSummary { Id = "1", Authority = "Auth1", ProviderType = "PType1" } }
                        }
                    }
                },
                new PublishedProviderResult
                {
                    Title = "Test 2",
                    Summary = "testing",
                    FundingPeriod = new Models.Specs.Period { Name = "Period1" },
                    FundingStreamResult = new PublishedFundingStreamResult
                    {
                        FundingStream = new Models.Reference { Name = "Stream2" },
                        AllocationLineResult = new PublishedAllocationLineResult
                        {
                            AllocationLine = new Models.Reference { Name = "AllocLine2" },
                            Current = new PublishedAllocationLineResultVersion { Value = 15, Provider = new ProviderSummary { Id = "2", Authority = "Auth2", ProviderType = "PType2" } }
                        }
                    }
                }
            };

            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);
            request
                .Body
                .Returns(stream);

            IPublishedProviderResultsRepository resultsProviderRepository = CreatePublishedProviderResultsRepository();
            resultsProviderRepository
                .GetPublishedProviderResultsForSpecificationAndStatus(Arg.Is(specificationId), Arg.Any<UpdatePublishedAllocationLineResultStatusModel>())
                .Returns(publishedProviderResults);

            ResultsService resultsService = CreateResultsService(publishedProviderResultsRepository: resultsProviderRepository);

            // Act
            IActionResult actionResult = await resultsService.GetConfirmationDetailsForApprovePublishProviderResults(request);

            // Assert
            AssertConfirmPublishApproveModel(actionResult, publishedProviderResults.Count(), 2, 2, "Period1", 2, 27);
        }

        [TestMethod]
        public async Task GetConfirmationDetailsForApprovePublishProviderResults_GivenPublishedResultsReturnsMultiple_ReturnsOkAndAggregatesResultsWhenSame()
        {
            // Arrange
            IEnumerable<UpdatePublishedAllocationLineResultStatusProviderModel> Providers = new[]
            {
                new UpdatePublishedAllocationLineResultStatusProviderModel
                {
                    ProviderId = "1"
                }
            };

            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "specificationId", new StringValues(specificationId) },
            });

            UpdatePublishedAllocationLineResultStatusModel model = new UpdatePublishedAllocationLineResultStatusModel
            {
                Status = AllocationLineStatus.Held,
                Providers = Providers
            };

            IEnumerable<PublishedProviderResult> publishedProviderResults = new List<PublishedProviderResult>
            {
                new PublishedProviderResult
                {
                    Title = "Test 1",
                    Summary = "testing",
                    FundingPeriod = new Models.Specs.Period { Name = "Period1" },
                    FundingStreamResult = new PublishedFundingStreamResult
                    {
                        FundingStream = new Models.Reference { Name = "Stream1" },
                        AllocationLineResult = new PublishedAllocationLineResult
                        {
                            AllocationLine = new Models.Reference { Name = "AllocLine1" },
                            Current = new PublishedAllocationLineResultVersion { Value = 12, Provider = new ProviderSummary { Id = "1", Authority = "Auth1", ProviderType = "PType1" } }
                        }
                    }
                },
                new PublishedProviderResult
                {
                    Title = "Test 2",
                    Summary = "testing",
                    FundingPeriod = new Models.Specs.Period { Name = "Period1" },
                    FundingStreamResult = new PublishedFundingStreamResult
                    {
                        FundingStream = new Models.Reference { Name = "Stream1" },
                        AllocationLineResult = new PublishedAllocationLineResult
                        {
                            AllocationLine = new Models.Reference { Name = "AllocLine2" },
                            Current = new PublishedAllocationLineResultVersion { Value = 15, Provider = new ProviderSummary { Id = "2", Authority = "Auth1", ProviderType = "PType1" } }
                        }
                    }
                }
            };

            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);
            request
                .Body
                .Returns(stream);

            IPublishedProviderResultsRepository resultsProviderRepository = CreatePublishedProviderResultsRepository();
            resultsProviderRepository
                .GetPublishedProviderResultsForSpecificationAndStatus(Arg.Is(specificationId), Arg.Any<UpdatePublishedAllocationLineResultStatusModel>())
                .Returns(publishedProviderResults);

            ResultsService resultsService = CreateResultsService(publishedProviderResultsRepository: resultsProviderRepository);

            // Act
            IActionResult actionResult = await resultsService.GetConfirmationDetailsForApprovePublishProviderResults(request);

            // Assert
            AssertConfirmPublishApproveModel(actionResult, publishedProviderResults.Count(), 1, 1, "Period1", 1, 27);
        }

        [TestMethod]
        public async Task GetConfirmationDetailsForApprovePublishProviderResults_GivenPublishedResultsReturnsMultiple_ReturnsOkAndProviderTypesOrdered()
        {
            // Arrange
            IEnumerable<UpdatePublishedAllocationLineResultStatusProviderModel> Providers = new[]
            {
                new UpdatePublishedAllocationLineResultStatusProviderModel { ProviderId = "1" },
                new UpdatePublishedAllocationLineResultStatusProviderModel { ProviderId = "2" }
            };

            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "specificationId", new StringValues(specificationId) },
            });

            UpdatePublishedAllocationLineResultStatusModel model = new UpdatePublishedAllocationLineResultStatusModel
            {
                Status = AllocationLineStatus.Held,
                Providers = Providers
            };

            IEnumerable<PublishedProviderResult> publishedProviderResults = new List<PublishedProviderResult>
            {
                new PublishedProviderResult
                {
                    Title = "Test 1",
                    Summary = "testing",
                    FundingPeriod = new Models.Specs.Period { Name = "Period1" },
                    FundingStreamResult = new PublishedFundingStreamResult
                    {
                        FundingStream = new Models.Reference { Name = "Stream1" },
                        AllocationLineResult = new PublishedAllocationLineResult
                        {
                            AllocationLine = new Models.Reference { Name = "AllocLine1" },
                            Current = new PublishedAllocationLineResultVersion { Value = 12, Provider = new ProviderSummary { Id = "1", Authority = "B Auth", ProviderType = "B PType" } }
                        }
                    }
                },
                new PublishedProviderResult
                {
                    Title = "Test 2",
                    Summary = "testing",
                    FundingPeriod = new Models.Specs.Period { Name = "Period1" },
                    FundingStreamResult = new PublishedFundingStreamResult
                    {
                        FundingStream = new Models.Reference { Name = "Stream1" },
                        AllocationLineResult = new PublishedAllocationLineResult
                        {
                            AllocationLine = new Models.Reference { Name = "AllocLine2" },
                            Current = new PublishedAllocationLineResultVersion { Value = 15, Provider = new ProviderSummary { Id = "2", Authority = "A Auth", ProviderType = "A PType" } }
                        }
                    }
                }
            };

            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);
            request
                .Body
                .Returns(stream);

            IPublishedProviderResultsRepository resultsProviderRepository = CreatePublishedProviderResultsRepository();
            resultsProviderRepository
                .GetPublishedProviderResultsForSpecificationAndStatus(Arg.Is(specificationId), Arg.Any<UpdatePublishedAllocationLineResultStatusModel>())
                .Returns(publishedProviderResults);

            ResultsService resultsService = CreateResultsService(publishedProviderResultsRepository: resultsProviderRepository);

            // Act
            IActionResult actionResult = await resultsService.GetConfirmationDetailsForApprovePublishProviderResults(request);

            // Assert
            OkObjectResult okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;

            ConfirmPublishApproveModel confDetalis = okResult.Value.Should().BeAssignableTo<ConfirmPublishApproveModel>().Subject;
            confDetalis.ProviderTypes.Should().HaveCount(2);
            confDetalis.ProviderTypes.ElementAt(0).Should().Be("A PType");
            confDetalis.ProviderTypes.ElementAt(1).Should().Be("B PType");
        }

        [TestMethod]
        public async Task GetConfirmationDetailsForApprovePublishProviderResults_GivenPublishedResultsReturnsMultiple_ReturnsOkAndAuthoritiesOrdered()
        {
            // Arrange
            IEnumerable<UpdatePublishedAllocationLineResultStatusProviderModel> Providers = new[]
            {
                new UpdatePublishedAllocationLineResultStatusProviderModel { ProviderId = "1" },
                new UpdatePublishedAllocationLineResultStatusProviderModel { ProviderId = "2" }
            };

            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "specificationId", new StringValues(specificationId) },
            });

            UpdatePublishedAllocationLineResultStatusModel model = new UpdatePublishedAllocationLineResultStatusModel
            {
                Status = AllocationLineStatus.Held,
                Providers = Providers
            };

            IEnumerable<PublishedProviderResult> publishedProviderResults = new List<PublishedProviderResult>
            {
                new PublishedProviderResult
                {
                    Title = "Test 1",
                    Summary = "testing",
                    FundingPeriod = new Models.Specs.Period { Name = "Period1" },
                    FundingStreamResult = new PublishedFundingStreamResult
                    {
                        FundingStream = new Models.Reference { Name = "Stream1" },
                        AllocationLineResult = new PublishedAllocationLineResult
                        {
                            AllocationLine = new Models.Reference { Name = "AllocLine1" },
                            Current = new PublishedAllocationLineResultVersion { Value = 12, Provider = new ProviderSummary { Id = "1", Authority = "B Auth", ProviderType = "B PType" } }
                        }
                    }
                },
                new PublishedProviderResult
                {
                    Title = "Test 2",
                    Summary = "testing",
                    FundingPeriod = new Models.Specs.Period { Name = "Period1" },
                    FundingStreamResult = new PublishedFundingStreamResult
                    {
                        FundingStream = new Models.Reference { Name = "Stream1" },
                        AllocationLineResult = new PublishedAllocationLineResult
                        {
                            AllocationLine = new Models.Reference { Name = "AllocLine2" },
                            Current = new PublishedAllocationLineResultVersion { Value = 15, Provider = new ProviderSummary { Id = "2", Authority = "A Auth", ProviderType = "A PType" } }
                        }
                    }
                }
            };

            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);
            request
                .Body
                .Returns(stream);

            IPublishedProviderResultsRepository resultsProviderRepository = CreatePublishedProviderResultsRepository();
            resultsProviderRepository
                .GetPublishedProviderResultsForSpecificationAndStatus(Arg.Is(specificationId), Arg.Any<UpdatePublishedAllocationLineResultStatusModel>())
                .Returns(publishedProviderResults);

            ResultsService resultsService = CreateResultsService(publishedProviderResultsRepository: resultsProviderRepository);

            // Act
            IActionResult actionResult = await resultsService.GetConfirmationDetailsForApprovePublishProviderResults(request);

            // Assert
            OkObjectResult okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;

            ConfirmPublishApproveModel confDetalis = okResult.Value.Should().BeAssignableTo<ConfirmPublishApproveModel>().Subject;
            confDetalis.LocalAuthorities.Should().HaveCount(2);
            confDetalis.LocalAuthorities.ElementAt(0).Should().Be("A Auth");
            confDetalis.LocalAuthorities.ElementAt(1).Should().Be("B Auth");
        }

        [TestMethod]
        public async Task GetConfirmationDetailsForApprovePublishProviderResults_GivenPublishedResultsReturnsMultiple_ReturnsOkAndAllocationLinesCoalesced()
        {
            // Arrange
            IEnumerable<UpdatePublishedAllocationLineResultStatusProviderModel> Providers = new[]
            {
                new UpdatePublishedAllocationLineResultStatusProviderModel { ProviderId = "1" },
                new UpdatePublishedAllocationLineResultStatusProviderModel { ProviderId = "2" }
            };

            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "specificationId", new StringValues(specificationId) },
            });

            UpdatePublishedAllocationLineResultStatusModel model = new UpdatePublishedAllocationLineResultStatusModel
            {
                Status = AllocationLineStatus.Held,
                Providers = Providers
            };

            IEnumerable<PublishedProviderResult> publishedProviderResults = new List<PublishedProviderResult>
            {
                new PublishedProviderResult
                {
                    Title = "Test 1",
                    Summary = "testing",
                    FundingPeriod = new Models.Specs.Period { Name = "Period1" },
                    FundingStreamResult = new PublishedFundingStreamResult
                    {
                        FundingStream = new Models.Reference { Name = "Stream1" },
                        AllocationLineResult = new PublishedAllocationLineResult
                        {
                            AllocationLine = new Models.Reference { Name = "AllocLine1" },
                            Current = new PublishedAllocationLineResultVersion { Value = 12, Provider = new ProviderSummary { Id = "1", Authority = "B Auth", ProviderType = "B PType" } }
                        }
                    }
                },
                new PublishedProviderResult
                {
                    Title = "Test 2",
                    Summary = "testing",
                    FundingPeriod = new Models.Specs.Period { Name = "Period1" },
                    FundingStreamResult = new PublishedFundingStreamResult
                    {
                        FundingStream = new Models.Reference { Name = "Stream1" },
                        AllocationLineResult = new PublishedAllocationLineResult
                        {
                            AllocationLine = new Models.Reference { Name = "AllocLine1" },
                            Current = new PublishedAllocationLineResultVersion { Value = 15, Provider = new ProviderSummary { Id = "2", Authority = "A Auth", ProviderType = "A PType" } }
                        }
                    }
                }
            };

            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);
            request
                .Body
                .Returns(stream);

            IPublishedProviderResultsRepository resultsProviderRepository = CreatePublishedProviderResultsRepository();
            resultsProviderRepository
                .GetPublishedProviderResultsForSpecificationAndStatus(Arg.Is(specificationId), Arg.Any<UpdatePublishedAllocationLineResultStatusModel>())
                .Returns(publishedProviderResults);

            ResultsService resultsService = CreateResultsService(publishedProviderResultsRepository: resultsProviderRepository);

            // Act
            IActionResult actionResult = await resultsService.GetConfirmationDetailsForApprovePublishProviderResults(request);

            // Assert
            OkObjectResult okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;

            ConfirmPublishApproveModel confDetalis = okResult.Value.Should().BeAssignableTo<ConfirmPublishApproveModel>().Subject;
            confDetalis.FundingStreams.Should().HaveCount(1, "Funding Stream");
            confDetalis.FundingStreams.First().AllocationLines.Should().HaveCount(1, "Allocation Lines");
        }

        private static void AssertConfirmPublishApproveModel(IActionResult actionResult, int expectedNumProviders, int expectedNumAuthorities, int expectedNumProviderTypes, string expectedFundingPeriod, int expectedNumFundingStreams, decimal expectedTotalApproved)
        {
            OkObjectResult okResult = actionResult
                .Should()
                .BeOfType<OkObjectResult>()
                .Subject;

            ConfirmPublishApproveModel x = okResult.Value.Should().BeAssignableTo<ConfirmPublishApproveModel>().Subject;
            x.NumberOfProviders.Should().Be(expectedNumProviders, nameof(x.NumberOfProviders));
            x.LocalAuthorities.Should().HaveCount(expectedNumAuthorities, nameof(x.LocalAuthorities));
            x.ProviderTypes.Should().HaveCount(expectedNumProviderTypes, nameof(x.ProviderTypes));
            x.FundingPeriod.Should().Be(expectedFundingPeriod, nameof(x.FundingPeriod));
            x.FundingStreams.Should().HaveCount(expectedNumFundingStreams, nameof(x.FundingStreams));
            x.TotalFundingApproved.Should().Be(expectedTotalApproved, nameof(x.TotalFundingApproved));
        }
    }
}
