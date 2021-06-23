using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Policies;
using CalculateFunding.Common.ApiClient.Policies.Models.FundingConfig;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.Models;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace CalculateFunding.Services.Publishing.UnitTests
{
    [TestClass]
    public class FundingConfigurationServiceTests
    {
        [TestMethod]
        public void GetFundingConfigurations_GivenNullApiResponse_ThrowsException()
        {
            //Arrange
            SpecificationSummary specificationSummary = CreateSpecificationSummary();

            IPoliciesApiClient policiesApiClient = CreatePoliciesApiClient();
            policiesApiClient
                .GetFundingConfiguration(Arg.Any<string>(), Arg.Any<string>())
                .Returns(
                    new ApiResponse<FundingConfiguration>(System.Net.HttpStatusCode.OK, new FundingConfiguration()),
                    (ApiResponse<FundingConfiguration>)null);

            FundingConfigurationService fundingConfigurationService = CreateService(policiesApiClient);

            //Act
            Func<Task> test = () => fundingConfigurationService.GetFundingConfigurations(specificationSummary);

            //Assert
            test
                .Should()
                .ThrowExactly<Exception>();
        }

        [TestMethod]
        public void GetFundingConfigurations_GivenApiResponseWithInvalidStatusCode_ThrowsException()
        {
            //Arrange
            SpecificationSummary specificationSummary = CreateSpecificationSummary();

            IPoliciesApiClient policiesApiClient = CreatePoliciesApiClient();
            policiesApiClient
                .GetFundingConfiguration(Arg.Any<string>(), Arg.Any<string>())
                .Returns(
                    new ApiResponse<FundingConfiguration>(System.Net.HttpStatusCode.OK, new FundingConfiguration()),
                    new ApiResponse<FundingConfiguration>(System.Net.HttpStatusCode.BadRequest, null, null));

            FundingConfigurationService fundingConfigurationService = CreateService(policiesApiClient);

            //Act
            Func<Task> test = () => fundingConfigurationService.GetFundingConfigurations(specificationSummary);

            //Assert
            test
                .Should()
                .ThrowExactly<Exception>();
        }

        [TestMethod]
        public async Task GetFundingConfigurations_GivenSuccessResponse_ReturnsDictionary()
        {
            //Arrange
            SpecificationSummary specificationSummary = CreateSpecificationSummary();

            IPoliciesApiClient policiesApiClient = CreatePoliciesApiClient();
            policiesApiClient
                .GetFundingConfiguration(Arg.Any<string>(), Arg.Any<string>())
                .Returns(new ApiResponse<FundingConfiguration>(System.Net.HttpStatusCode.OK, new FundingConfiguration()));

            FundingConfigurationService fundingConfigurationService = CreateService(policiesApiClient);

            //Act
            IDictionary<string, FundingConfiguration> results = await fundingConfigurationService.GetFundingConfigurations(specificationSummary);

            //Assert
            results
                .Should()
                .HaveCount(3);

            results["fs-1"]
                .Should()
                .NotBeNull();

            results["fs-2"]
                .Should()
                .NotBeNull();

            results["fs-3"]
                .Should()
                .NotBeNull();
        }

        private static FundingConfigurationService CreateService(
                IPoliciesApiClient policiesApiClient = null)
        {
            return new FundingConfigurationService(
                policiesApiClient ?? CreatePoliciesApiClient(),
                PublishingResilienceTestHelper.GenerateTestPolicies());
        }

        private static IPoliciesApiClient CreatePoliciesApiClient()
        {
            return Substitute.For<IPoliciesApiClient>();
        }

        private static SpecificationSummary CreateSpecificationSummary()
        {
            return new SpecificationSummary
            {
                FundingPeriod = new Reference
                {
                    Id = "fp-1"
                },
                FundingStreams = new[]
                {
                    new Reference { Id = "fs-1"},
                    new Reference { Id = "fs-2"},
                    new Reference { Id = "fs-3"},
                }
            };
        }
    }
}
