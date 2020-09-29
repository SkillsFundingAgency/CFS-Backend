using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Calcs;
using CalculateFunding.Common.ApiClient.Calcs.Models;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Services.Core;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Serilog;

namespace CalculateFunding.Services.Publishing.UnitTests.Services
{
    [TestClass]
    public class CalculationsServiceTests
    {
        private const string specificationId = "spec-id-1";
        private const string fundingStreamId = "funding-stream-id-1";

        [TestMethod]
        public void HasAllTemplateCalculationsBeenApproved_GivenFailedResponseCheckingForApprovedTemplateCalcs_ThrowsRetriableException()
        {
            //Arrange
            ApiResponse<BooleanResponseModel> apiResponse = new ApiResponse<BooleanResponseModel>(HttpStatusCode.NotFound);

            ICalculationsApiClient calculationsApiClient = CreateCalculationsApiClient();
            calculationsApiClient
                .CheckHasAllApprovedTemplateCalculationsForSpecificationId(Arg.Is(specificationId))
                .Returns(apiResponse);

            ILogger logger = CreateLogger();

            string errorMessage = $"Failed to check spoecification with id '{specificationId}' " +
                    $"for all approved template calculations with status code '{HttpStatusCode.NotFound}'";

            CalculationsService calculationsService = CreateCalculationsService(calculationsApiClient, logger);

            //Act
            Func<Task> test = async () => await calculationsService.HaveAllTemplateCalculationsBeenApproved(specificationId);

            //Assert
            test
                .Should()
                .ThrowExactly<RetriableException>()
                .Which
                .Message
                .Should()
                .Be(errorMessage);

            logger
                .Received(1)
                .Error(Arg.Is(errorMessage));
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public async Task HasAllTemplateCalculationsBeenApproved_GivenResponseIsSuccess_ReturnsValue(bool expectedValue)
        {
            //Arrange
            BooleanResponseModel booleanResponseModel = new BooleanResponseModel { Value = expectedValue };

            ApiResponse<BooleanResponseModel> apiResponse = new ApiResponse<BooleanResponseModel>(HttpStatusCode.OK, booleanResponseModel);

            ICalculationsApiClient calculationsApiClient = CreateCalculationsApiClient();
            calculationsApiClient
                .CheckHasAllApprovedTemplateCalculationsForSpecificationId(Arg.Is(specificationId))
                .Returns(apiResponse);

            CalculationsService calculationsService = CreateCalculationsService(calculationsApiClient);

            //Act
            bool responseValue = await calculationsService.HaveAllTemplateCalculationsBeenApproved(specificationId);

            //Assert
            responseValue
                .Should()
                .Be(expectedValue);
        }

        [TestMethod]
        public async Task GetTemplateMapping_GivenFailedResponseFromCalculationsApi_ThrowsRetriableException()
        {
            //Arrange
            ApiResponse<TemplateMapping> apiResponse = new ApiResponse<TemplateMapping>(HttpStatusCode.NotFound);

            ICalculationsApiClient calculationsApiClient = CreateCalculationsApiClient();
            calculationsApiClient
                .GetTemplateMapping(Arg.Is(specificationId), Arg.Is(fundingStreamId))
                .Returns(apiResponse);

            ILogger logger = CreateLogger();

            string errorMessage = $"Failed to retrieve template mapping for specification id '{specificationId}' and  funding stream id '{fundingStreamId}'" +
                    $" with status code '{apiResponse.StatusCode}'";

            CalculationsService calculationsService = CreateCalculationsService(calculationsApiClient, logger);

            //Act
            Func<Task> test = async () => await calculationsService.GetTemplateMapping(specificationId, fundingStreamId);

            //Assert
            test
                .Should()
                .ThrowExactly<RetriableException>()
                .Which
                .Message
                .Should()
                .Be(errorMessage);

            logger
                .Received(1)
                .Error(Arg.Is(errorMessage));
        }

        [TestMethod]
        public async Task GetTemplateMapping_GivenResponseIsSuccess_ReturnsTempalteMappingValue()
        {
            //Arrange
            TemplateMapping expectedTemplateMapping = new TemplateMapping() { SpecificationId = specificationId, FundingStreamId = fundingStreamId };
            ApiResponse<TemplateMapping> apiResponse = new ApiResponse<TemplateMapping>(HttpStatusCode.OK, expectedTemplateMapping);

            ICalculationsApiClient calculationsApiClient = CreateCalculationsApiClient();
            calculationsApiClient
                .GetTemplateMapping(Arg.Is(specificationId), Arg.Is(fundingStreamId))
                .Returns(apiResponse);

            ILogger logger = CreateLogger();

            CalculationsService calculationsService = CreateCalculationsService(calculationsApiClient, logger);

            //Act
            TemplateMapping responseValue = await calculationsService.GetTemplateMapping(specificationId, fundingStreamId);

            //Assert
            responseValue
                .Should()
                .Be(expectedTemplateMapping);
        }

        [TestMethod]
        public async Task GetCalculationMetadataForSpecification_GivenFailedResponseFromCalculationsApi_ThrowsRetriableException()
        {
            //Arrange
            ApiResponse<IEnumerable<CalculationMetadata>> apiResponse = new ApiResponse<IEnumerable<CalculationMetadata>>(HttpStatusCode.NotFound);

            ICalculationsApiClient calculationsApiClient = CreateCalculationsApiClient();
            calculationsApiClient
                .GetCalculationMetadataForSpecification(Arg.Is(specificationId))
                .Returns(apiResponse);

            ILogger logger = CreateLogger();

            string errorMessage = $"Failed to retrieve calculation metadata for specification id '{specificationId}'" +
                    $" with status code '{apiResponse.StatusCode}'";

            CalculationsService calculationsService = CreateCalculationsService(calculationsApiClient, logger);

            //Act
            Func<Task> test = async () => await calculationsService.GetCalculationMetadataForSpecification(specificationId);

            //Assert
            test
                .Should()
                .ThrowExactly<RetriableException>()
                .Which
                .Message
                .Should()
                .Be(errorMessage);

            logger
                .Received(1)
                .Error(Arg.Is(errorMessage));
        }

        [TestMethod]
        public async Task GetCalculationMetadataForSpecification_GivenResponseIsSuccess_ReturnsCalculationMetadataValue()
        {
            //Arrange
            IEnumerable<CalculationMetadata> expectedCalculationMetadata = new List<CalculationMetadata>() { new CalculationMetadata() };
            ApiResponse<IEnumerable<CalculationMetadata>> apiResponse = new ApiResponse<IEnumerable<CalculationMetadata>>(HttpStatusCode.OK, expectedCalculationMetadata);

            ICalculationsApiClient calculationsApiClient = CreateCalculationsApiClient();
            calculationsApiClient
                .GetCalculationMetadataForSpecification(Arg.Is(specificationId))
                .Returns(apiResponse);

            ILogger logger = CreateLogger();

            CalculationsService calculationsService = CreateCalculationsService(calculationsApiClient, logger);

            //Act
            IEnumerable<CalculationMetadata> responseValue = await calculationsService.GetCalculationMetadataForSpecification(specificationId);

            //Assert
            responseValue
                .Should()
                .BeEquivalentTo(expectedCalculationMetadata);
        }

        public CalculationsService CreateCalculationsService(
            ICalculationsApiClient calculationsApiClient = null,
            ILogger logger = null)
        {
            return new CalculationsService(
                calculationsApiClient ?? CreateCalculationsApiClient(),
                PublishingResilienceTestHelper.GenerateTestPolicies(),
                logger ?? CreateLogger());
        }

        private static ICalculationsApiClient CreateCalculationsApiClient()
        {
            return Substitute.For<ICalculationsApiClient>();
        }

        private static ILogger CreateLogger()
        {
            return Substitute.For<ILogger>();
        }
    }
}
