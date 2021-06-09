using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Policies;
using CalculateFunding.Common.ApiClient.Policies.Models.FundingConfig;
using CalculateFunding.Common.ApiClient.Specifications;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Datasets.UnitTests.Builders;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Datasets
{
    [TestClass]
    public class SpecificationsServiceTests
    {
        private SpecificationsService _specificationsService;
        private Mock<ISpecificationsApiClient> _specificationsApiClient;
        private Mock<IPoliciesApiClient> _policiesApiClient;
        private Mock<ILogger> _logger;

        [TestInitialize]
        public void Initialize()
        {
            _specificationsApiClient = new Mock<ISpecificationsApiClient>();
            _policiesApiClient = new Mock<IPoliciesApiClient>();
            _logger = new Mock<ILogger>();

            _specificationsService = new SpecificationsService(
                _specificationsApiClient.Object,
                _policiesApiClient.Object,
                DatasetsResilienceTestHelper.GenerateTestPolicies(),
                _logger.Object
                );
        }

        [TestMethod]
        public async Task GetEligibleSpecificationsToReference_ShouldThrowArgumentNullException_WhenNullSpecificationIdSent()
        {
            // Act
            Func<Task> func =
                async () => await WhenGetEligibleSpecificationsToReference(null);

            // Assert
            func
                .Should()
                .ThrowExactly<ArgumentNullException>();
        }

        [TestMethod]
        public async Task GetEligibleSpecificationsToReference_ShouldThrowRetriableException_WhenGetSpecificationSummaryByIdThrowsError()
        {
            string specificationId = NewRandomString();

            GivenGetSpecificationSummaryByIdError(specificationId);

            Func<Task> func =
                async () => await WhenGetEligibleSpecificationsToReference(specificationId);

            func
                .Should()
                .ThrowExactly<RetriableException>();
        }

        [TestMethod]
        public async Task GetEligibleSpecificationsToReference_ShouldThrowRetriableException_WhenGetFundingConfigurationThrowsError()
        {
            string specificationId = NewRandomString();
            string fundingPeriodId = NewRandomString();
            string fundingStreamId = NewRandomString();

            SpecificationSummary specificationSummary = NewSpecificationSummary(_ => _
                .WithFundingStreamIds(fundingStreamId)
                .WithFundingPeriodId(fundingPeriodId));

            GivenGetSpecificationSummaryById(specificationId, specificationSummary);
            AndGetFundingConfigurationError(fundingPeriodId, fundingStreamId);

            Func<Task> func =
                async () => await WhenGetEligibleSpecificationsToReference(specificationId);

            func
                .Should()
                .ThrowExactly<RetriableException>();
        }

        [TestMethod]
        public async Task GetEligibleSpecificationsToReference_ShouldThrowRetriableException_WhenGetSpecificationsSelectedForFundingThrowsError()
        {
            string specificationId = NewRandomString();
            string fundingPeriodId = NewRandomString();
            string fundingStreamId = NewRandomString();

            SpecificationSummary specificationSummary = NewSpecificationSummary(_ => _
                .WithFundingStreamIds(fundingStreamId)
                .WithFundingPeriodId(fundingPeriodId));
            
            FundingConfiguration fundingConfiguration = NewFundingConfiguration(_ => _.WithAllowedPublishedFundingStreamsIdsToReference(Array.Empty<string>()));

            GivenGetSpecificationSummaryById(specificationId, specificationSummary);
            AndGetFundingConfiguration(fundingPeriodId, fundingStreamId, fundingConfiguration);
            AndGetSpecificationsSelectedForFundingError();

            Func<Task> func =
                async () => await WhenGetEligibleSpecificationsToReference(specificationId);

            func
                .Should()
                .ThrowExactly<RetriableException>();
        }

        [TestMethod]
        public async Task GetEligibleSpecificationsToReference_ShouldReturnEligibleSpecificationReferences_WhenCalled()
        {
            string specificationId = NewRandomString();
            string fundingPeriodId = NewRandomString();
            string fundingStreamId = NewRandomString();

            string eligibleSpecificationId = NewRandomString();
            string eligibleFundingPeriodId = NewRandomString();
            string eligibleFundingStreamId = NewRandomString();

            SpecificationSummary specificationSummary = NewSpecificationSummary(_ => _
                .WithFundingStreamIds(fundingStreamId)
                .WithFundingPeriodId(fundingPeriodId));

            SpecificationSummary eligibleSpecificationSummary = NewSpecificationSummary(_ => _
                .WithFundingStreamIds(eligibleFundingStreamId)
                .WithFundingPeriodId(eligibleFundingPeriodId)
                .WithId(eligibleSpecificationId));

            FundingConfiguration fundingConfiguration = NewFundingConfiguration(_ => _.WithAllowedPublishedFundingStreamsIdsToReference(new[] { eligibleFundingStreamId }));

            GivenGetSpecificationSummaryById(specificationId, specificationSummary);
            AndGetFundingConfiguration(fundingPeriodId, fundingStreamId, fundingConfiguration);
            AndGetSpecificationsSelectedForFunding(new[] { eligibleSpecificationSummary });

            IActionResult actionResult = await WhenGetEligibleSpecificationsToReference(specificationId);

            actionResult
                .Should()
                .BeOfType<OkObjectResult>()
                .Subject
                .Value
                .Should()
                .BeOfType<List<EligibleSpecificationReference>>();

            List<EligibleSpecificationReference> eligibleSpecificationReferences = (actionResult as OkObjectResult).Value as List<EligibleSpecificationReference>;
            EligibleSpecificationReference eligibleSpecificationReference = eligibleSpecificationReferences.SingleOrDefault();

            eligibleSpecificationReference.SpecificationId.Should().Be(eligibleSpecificationId);
            eligibleSpecificationReference.FundingPeriodId.Should().Be(eligibleFundingPeriodId);
            eligibleSpecificationReference.FundingStreamId.Should().Be(eligibleFundingStreamId);
        }

        private void GivenGetSpecificationSummaryById(string specificationId, SpecificationSummary specificationSummary) =>
            _specificationsApiClient
                .Setup(_ => _.GetSpecificationSummaryById(specificationId))
                .ReturnsAsync(new ApiResponse<SpecificationSummary>(HttpStatusCode.OK, specificationSummary));

        private void GivenGetSpecificationSummaryByIdError(string specificationId) =>
            _specificationsApiClient
                .Setup(_ => _.GetSpecificationSummaryById(specificationId))
                .ReturnsAsync(new ApiResponse<SpecificationSummary>(HttpStatusCode.InternalServerError));

        private void AndGetFundingConfiguration(string fundingPeriodId, string fundingStreamId, FundingConfiguration fundingConfiguration) =>
            _policiesApiClient
                .Setup(_ => _.GetFundingConfiguration(fundingStreamId, fundingPeriodId))
                .ReturnsAsync(new ApiResponse<FundingConfiguration>(HttpStatusCode.OK, fundingConfiguration));

        private void AndGetFundingConfigurationError(string fundingPeriodId, string fundingStreamId) =>
            _policiesApiClient
                .Setup(_ => _.GetFundingConfiguration(fundingStreamId, fundingPeriodId))
                .ReturnsAsync(new ApiResponse<FundingConfiguration>(HttpStatusCode.InternalServerError));

        private void AndGetSpecificationsSelectedForFunding(IEnumerable<SpecificationSummary> specificationSummaries) =>
            _specificationsApiClient
                .Setup(_ => _.GetSpecificationsSelectedForFunding())
                .ReturnsAsync(new ApiResponse<IEnumerable<SpecificationSummary>>(HttpStatusCode.OK, specificationSummaries));

        private void AndGetSpecificationsSelectedForFundingError() =>
            _specificationsApiClient
                .Setup(_ => _.GetSpecificationsSelectedForFunding())
                .ReturnsAsync(new ApiResponse<IEnumerable<SpecificationSummary>>(HttpStatusCode.InternalServerError));


        private async Task<IActionResult> WhenGetEligibleSpecificationsToReference(string specificationId) =>
            await _specificationsService.GetEligibleSpecificationsToReference(specificationId);

        private SpecificationSummary NewSpecificationSummary(Action<SpecificationSummaryBuilder> setUp = null)
        {
            SpecificationSummaryBuilder specificationSummaryBuilder = new SpecificationSummaryBuilder();

            setUp?.Invoke(specificationSummaryBuilder);

            return specificationSummaryBuilder.Build();
        }

        private FundingConfiguration NewFundingConfiguration(Action<FundingConfigurationBuilder> setUp = null)
        {
            FundingConfigurationBuilder fundingConfigurationBuilder = new FundingConfigurationBuilder();

            setUp?.Invoke(fundingConfigurationBuilder);

            return fundingConfigurationBuilder.Build();
        }

        private string NewRandomString() => new RandomString();
    }
}
