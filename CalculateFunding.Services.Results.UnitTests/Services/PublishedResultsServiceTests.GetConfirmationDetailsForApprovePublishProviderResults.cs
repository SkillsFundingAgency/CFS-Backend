using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

namespace CalculateFunding.Services.Results.Services
{
    public partial class PublishedResultsServiceTests
    {
        [TestMethod]
        [DataRow("", "", "", "providerId", "No Provider ID provided")]
        [DataRow(" ", "", "", "providerId", "No Provider ID provided")]
        [DataRow("p", "", "", "specificationId", "No Specification ID provided")]
        [DataRow("p", " ", "", "specificationId", "No Specification ID provided")]
        [DataRow("p", "s", "", "fundingStreamId", "No Funding Stream ID provided")]
        [DataRow("p", "s", " ", "fundingStreamId", "No Funding Stream ID provided")]
        public void GetPublishedProviderProfileForProviderIdAndSpecificationIdAndFundingStreamId_MissingData_LogsAndThrowsException(
            string providerId,
            string specificationId,
            string fundingStreamId,
            string parameterName,
            string message)
        {
            //Arrange
            ILogger logger = Substitute.For<ILogger>();
            PublishedResultsService service = CreateResultsService(logger);

            //Act
            Func<Task> action = async () =>
                await service.GetPublishedProviderProfileForProviderIdAndSpecificationIdAndFundingStreamId(providerId, specificationId, fundingStreamId);

            //Assert
            action
                .Should().Throw<ArgumentNullException>()
                .WithMessage($"{message}{Environment.NewLine}Parameter name: {parameterName}");

            logger
                .Received(1)
                .Error(message);
        }

        [TestMethod]
        public async Task GetPublishedProviderProfileForProviderIdAndSpecificationIdAndFundingStreamId_ValidData_ReturnsData()
        {
            string providerId = "123";
            string specificationId = "456";
            string fundingStreamId = "789";

            IPublishedProviderResultsRepository publishedProviderResultsRepository = Substitute.For<IPublishedProviderResultsRepository>();
            IEnumerable<PublishedProviderProfileViewModel> returnData = new[] { new PublishedProviderProfileViewModel() };

            publishedProviderResultsRepository
                .GetPublishedProviderProfileForProviderIdAndSpecificationIdAndFundingStreamId(providerId, specificationId, fundingStreamId)
                .Returns(returnData);

            PublishedResultsService service = CreateResultsService(publishedProviderResultsRepository: publishedProviderResultsRepository);
            IActionResult result = await service.GetPublishedProviderProfileForProviderIdAndSpecificationIdAndFundingStreamId(providerId, specificationId, fundingStreamId);

            await publishedProviderResultsRepository
                .Received(1)
                .GetPublishedProviderProfileForProviderIdAndSpecificationIdAndFundingStreamId(providerId, specificationId, fundingStreamId);

            result.Should().BeOfType<OkObjectResult>();
            (result as OkObjectResult).Value.Should().Be(returnData);
        }

        [TestMethod]
        public async Task GetPublishedProviderProfileForProviderIdAndSpecificationIdAndFundingStreamId_RepoReturnsEmpty_ReturnsNotFoundResult()
        {
            string providerId = "123";
            string specificationId = "456";
            string fundingStreamId = "789";

            IPublishedProviderResultsRepository publishedProviderResultsRepository = Substitute.For<IPublishedProviderResultsRepository>();

            PublishedResultsService service = CreateResultsService(publishedProviderResultsRepository: publishedProviderResultsRepository);
            IActionResult result = await service.GetPublishedProviderProfileForProviderIdAndSpecificationIdAndFundingStreamId(providerId, specificationId, fundingStreamId);

            await publishedProviderResultsRepository
                .Received(1)
                .GetPublishedProviderProfileForProviderIdAndSpecificationIdAndFundingStreamId(providerId, specificationId, fundingStreamId);

            result.Should().BeOfType<NotFoundResult>();
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

            PublishedResultsService resultsService = CreateResultsService(publishedProviderResultsRepository: resultsProviderRepository);

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
                    FundingPeriod = new Models.Specs.Period { Name = "Period1" },
                    FundingStreamResult = new PublishedFundingStreamResult
                    {
                        FundingStream = new PublishedFundingStreamDefinition { Name = "Stream1" },
                        AllocationLineResult = new PublishedAllocationLineResult
                        {
                            AllocationLine = new PublishedAllocationLineDefinition { Name = "AllocLine1" },
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

            PublishedResultsService resultsService = CreateResultsService(publishedProviderResultsRepository: resultsProviderRepository);

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
                    FundingPeriod = new Models.Specs.Period { Name = "Period1" },
                    FundingStreamResult = new PublishedFundingStreamResult
                    {
                        FundingStream = new PublishedFundingStreamDefinition { Name = "Stream1" },
                        AllocationLineResult = new PublishedAllocationLineResult
                        {
                            AllocationLine = new PublishedAllocationLineDefinition { Name = "AllocLine1" },
                            Current = new PublishedAllocationLineResultVersion { Value = 12, Provider = new ProviderSummary { Id = "1", Authority = "Auth1", ProviderType = "PType1" } }
                        }
                    }
                },
                new PublishedProviderResult
                {
                    FundingPeriod = new Models.Specs.Period { Name = "Period1" },
                    FundingStreamResult = new PublishedFundingStreamResult
                    {
                        FundingStream = new PublishedFundingStreamDefinition { Name = "Stream2" },
                        AllocationLineResult = new PublishedAllocationLineResult
                        {
                            AllocationLine = new PublishedAllocationLineDefinition { Name = "AllocLine2" },
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

            PublishedResultsService resultsService = CreateResultsService(publishedProviderResultsRepository: resultsProviderRepository);

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
                    FundingPeriod = new Models.Specs.Period { Name = "Period1" },
                    FundingStreamResult = new PublishedFundingStreamResult
                    {
                        FundingStream = new PublishedFundingStreamDefinition { Name = "Stream1" },
                        AllocationLineResult = new PublishedAllocationLineResult
                        {
                            AllocationLine = new PublishedAllocationLineDefinition { Name = "AllocLine1" },
                            Current = new PublishedAllocationLineResultVersion { Value = 12, Provider = new ProviderSummary { Id = "1", Authority = "Auth1", ProviderType = "PType1" } }
                        }
                    }
                },
                new PublishedProviderResult
                {
                    FundingPeriod = new Models.Specs.Period { Name = "Period1" },
                    FundingStreamResult = new PublishedFundingStreamResult
                    {
                        FundingStream = new PublishedFundingStreamDefinition { Name = "Stream1" },
                        AllocationLineResult = new PublishedAllocationLineResult
                        {
                            AllocationLine = new PublishedAllocationLineDefinition { Name = "AllocLine2" },
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

            PublishedResultsService resultsService = CreateResultsService(publishedProviderResultsRepository: resultsProviderRepository);

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
                    FundingPeriod = new Models.Specs.Period { Name = "Period1" },
                    FundingStreamResult = new PublishedFundingStreamResult
                    {
                        FundingStream = new PublishedFundingStreamDefinition { Name = "Stream1" },
                        AllocationLineResult = new PublishedAllocationLineResult
                        {
                            AllocationLine = new PublishedAllocationLineDefinition { Name = "AllocLine1" },
                            Current = new PublishedAllocationLineResultVersion { Value = 12, Provider = new ProviderSummary { Id = "1", Authority = "B Auth", ProviderType = "B PType" } }
                        }
                    }
                },
                new PublishedProviderResult
                {
                    FundingPeriod = new Models.Specs.Period { Name = "Period1" },
                    FundingStreamResult = new PublishedFundingStreamResult
                    {
                        FundingStream = new PublishedFundingStreamDefinition { Name = "Stream1" },
                        AllocationLineResult = new PublishedAllocationLineResult
                        {
                            AllocationLine = new PublishedAllocationLineDefinition { Name = "AllocLine2" },
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

            PublishedResultsService resultsService = CreateResultsService(publishedProviderResultsRepository: resultsProviderRepository);

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
                    FundingPeriod = new Models.Specs.Period { Name = "Period1" },
                    FundingStreamResult = new PublishedFundingStreamResult
                    {
                        FundingStream = new PublishedFundingStreamDefinition { Name = "Stream1" },
                        AllocationLineResult = new PublishedAllocationLineResult
                        {
                            AllocationLine = new PublishedAllocationLineDefinition { Name = "AllocLine1" },
                            Current = new PublishedAllocationLineResultVersion { Value = 12, Provider = new ProviderSummary { Id = "1", Authority = "B Auth", ProviderType = "B PType" } }
                        }
                    }
                },
                new PublishedProviderResult
                {
                    FundingPeriod = new Models.Specs.Period { Name = "Period1" },
                    FundingStreamResult = new PublishedFundingStreamResult
                    {
                        FundingStream = new PublishedFundingStreamDefinition { Name = "Stream1" },
                        AllocationLineResult = new PublishedAllocationLineResult
                        {
                            AllocationLine = new PublishedAllocationLineDefinition { Name = "AllocLine2" },
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

            PublishedResultsService resultsService = CreateResultsService(publishedProviderResultsRepository: resultsProviderRepository);

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
                    FundingPeriod = new Models.Specs.Period { Name = "Period1" },
                    FundingStreamResult = new PublishedFundingStreamResult
                    {
                        FundingStream = new PublishedFundingStreamDefinition { Name = "Stream1" },
                        AllocationLineResult = new PublishedAllocationLineResult
                        {
                            AllocationLine = new PublishedAllocationLineDefinition { Name = "AllocLine1" },
                            Current = new PublishedAllocationLineResultVersion { Value = 12, Provider = new ProviderSummary { Id = "1", Authority = "B Auth", ProviderType = "B PType" } }
                        }
                    }
                },
                new PublishedProviderResult
                {
                    FundingPeriod = new Models.Specs.Period { Name = "Period1" },
                    FundingStreamResult = new PublishedFundingStreamResult
                    {
                        FundingStream = new PublishedFundingStreamDefinition { Name = "Stream1" },
                        AllocationLineResult = new PublishedAllocationLineResult
                        {
                            AllocationLine = new PublishedAllocationLineDefinition { Name = "AllocLine1" },
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

            PublishedResultsService resultsService = CreateResultsService(publishedProviderResultsRepository: resultsProviderRepository);

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
