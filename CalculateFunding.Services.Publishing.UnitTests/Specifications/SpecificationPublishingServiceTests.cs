﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Policies.Models;
using CalculateFunding.Common.Caching;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;
using CalculateFunding.Services.Publishing.Specifications;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using ApiJob = CalculateFunding.Common.ApiClient.Jobs.Models.Job;
using ApiSpecificationSummary = CalculateFunding.Common.ApiClient.Specifications.Models.SpecificationSummary;

namespace CalculateFunding.Services.Publishing.UnitTests.Specifications
{
    [TestClass]
    public class SpecificationPublishingServiceTests : SpecificationPublishingServiceTestsBase<ICreateRefreshFundingJobs>
    {
        private SpecificationPublishingService _service;
        private ICreateApproveAllFundingJobs _approveSpecificationFundingJobs;
        private ICreateApproveBatchFundingJobs _approveProviderFundingJobs;
        private ICacheProvider _cacheProvider;
        private ISpecificationFundingStatusService _specificationFundingStatusService;
        private PublishedProviderIdsRequest _approveProvidersRequest;
        private IPrerequisiteCheckerLocator _prerequisiteCheckerLocator;
        private IPrerequisiteChecker _prerequisiteChecker;

        [TestInitialize]
        public void SetUp()
        {
            _approveSpecificationFundingJobs = Substitute.For<ICreateApproveAllFundingJobs>();
            _approveProviderFundingJobs = Substitute.For<ICreateApproveBatchFundingJobs>();
            _cacheProvider = Substitute.For<ICacheProvider>();
            _specificationFundingStatusService = Substitute.For<ISpecificationFundingStatusService>();
            _prerequisiteCheckerLocator = Substitute.For<IPrerequisiteCheckerLocator>();
            _prerequisiteChecker = Substitute.For<IPrerequisiteChecker>();

            _service = new SpecificationPublishingService(
                SpecificationIdValidator,
                ProviderIdsValidator,
                Specifications,
                ResiliencePolicies,
                _cacheProvider,
                Jobs,
                _approveSpecificationFundingJobs,
                _approveProviderFundingJobs,
                _specificationFundingStatusService,
                FundingConfigurationService,
                _prerequisiteCheckerLocator);

            _approveProvidersRequest = BuildApproveProvidersRequest(_ => _.WithProviders(ProviderIds));
        }

        [TestMethod]
        public async Task ReturnsBadRequestWhenSuppliedSpecificationIdFailsValidation()
        {
            string[] expectedErrors = { NewRandomString(), NewRandomString() };

            GivenTheValidationErrors(expectedErrors);

            await WhenTheSpecificationIsPublished();

            ThenTheResponseShouldBe<BadRequestObjectResult>();
        }

        [TestMethod]
        public async Task ReturnsNotFoundResultIfNoSpecificationLocatedWithTheSuppliedId()
        {
            GivenTheApiResponseDetailsForTheSuppliedId(null, HttpStatusCode.NotFound);

            await WhenTheSpecificationIsPublished();

            ThenTheResponseShouldBe<NotFoundResult>();
        }

        [TestMethod]
        public async Task ReturnsStatusCode409IfAnotherSpecificationIsPublishedForTheSameFundingStreamAndPeriod()
        {
            string commonFundingStreamId = NewRandomString();
            string fundingPeriodId = NewRandomString();

            ApiSpecificationSummary specificationSummary = NewApiSpecificationSummary(_ =>
                _.WithFundingStreamIds(commonFundingStreamId,
                        NewRandomString(),
                        NewRandomString())
                    .WithFundingPeriodId(fundingPeriodId));

            GivenTheApiResponseDetailsForTheSuppliedId(specificationSummary);
            AndTheApiResponseDetailsForTheFundingPeriodId(fundingPeriodId,
                NewApiSpecificationSummary(_ => _.WithFundingStreamIds(NewRandomString(), commonFundingStreamId)),
                NewApiSpecificationSummary(_ => _.WithFundingStreamIds(NewRandomString())));

            AndTheSpecificationSummaryConflictsWithAnotherForThatFundingPeriod(specificationSummary);


            await WhenTheSpecificationIsPublished();

            ThenTheResponseShouldBe<ConflictResult>();
        }

        [TestMethod]
        public async Task CreatesPublishSpecificationJobForSuppliedSpecificationId()
        {
            string fundingPeriodId = NewRandomString();

            ApiSpecificationSummary specificationSummary = NewApiSpecificationSummary(_ =>
                _.WithFundingStreamIds(NewRandomString(),
                        NewRandomString(),
                        NewRandomString())
                    .WithFundingPeriodId(fundingPeriodId));

            string refreshFundingJobId = NewRandomString();
            ApiJob refreshFundingJob = NewJob(_ => _.WithId(refreshFundingJobId));

            GivenTheApiResponseDetailsForTheSuppliedId(specificationSummary);
            AndTheApiResponseDetailsForTheFundingPeriodId(fundingPeriodId);
            AndGetPreReqCheckerLocatorReturnsRefreshPreReqChecker();
            AndTheApiResponseDetailsForSpecificationsJob(refreshFundingJob);

            JobCreationResponse expectedJobCreationResponse = NewJobCreationResponse(_ => _.WithJobId(refreshFundingJobId));

            await WhenTheSpecificationIsPublished();

            ActionResult
                .Should()
                .BeOfType<OkObjectResult>()
                .Which
                .Value
                .Should()
                .BeEquivalentTo(expectedJobCreationResponse);
        }

        [TestMethod]
        public async Task Returns400IfPrereqChecksFails()
        {
            string fundingPeriodId = NewRandomString();
            string preReqCheckFailedErrorMessage = "Prerequisite check for refresh failed Error in the application." ;

            ApiSpecificationSummary specificationSummary =
                NewApiSpecificationSummary(_ => _
                    .WithIsSelectedForFunding(true)
                    .WithFundingPeriodId(fundingPeriodId)
                    .WithFundingStreamIds(NewRandomString(),
                        NewRandomString(),
                        NewRandomString()));

            GivenTheApiResponseDetailsForTheSuppliedId(specificationSummary);
            AndTheApiResponseDetailsForTheFundingPeriodId(fundingPeriodId);
            AndGetPreReqCheckerLocatorReturnsRefreshPreReqChecker();
            AndPrereqChecksFails(specificationSummary);

            await WhenTheSpecificationIsPublished();

            var result = ActionResult
                .Should()
                .BeOfType<BadRequestObjectResult>()
                .Which
                .Value;

            ((result as ICollection<KeyValuePair<string, object>>).First().Value as string[]).First()
                .Should()
                .Be(preReqCheckFailedErrorMessage);
        }

        [TestMethod]
        public async Task ReturnsBadRequestWhenSuppliedSpecificationIdFailsValidationForSpecificationApproval()
        {
            string[] expectedErrors = { NewRandomString(), NewRandomString() };

            GivenTheValidationErrors(expectedErrors);

            await WhenAllProvidersAreApproved();

            ThenTheResponseShouldBe<BadRequestObjectResult>();
        }

        [TestMethod]
        public async Task ReturnsBadRequestWhenSuppliedSpecificationIdFailsValidationForProvidersApproval()
        {
            string[] expectedErrors = { NewRandomString(), NewRandomString() };

            GivenTheValidationErrors(expectedErrors);

            await WhenBatchProvidersAreApproved();

            ThenTheResponseShouldBe<BadRequestObjectResult>();
        }

        [TestMethod]
        public async Task ReturnsNotFoundResultIfNoSpecificationLocatedWithTheSuppliedIdForSpecificationApproval()
        {
            GivenTheApiResponseDetailsForTheSuppliedId(null, HttpStatusCode.NotFound);

            await WhenAllProvidersAreApproved();

            ThenTheResponseShouldBe<NotFoundResult>();
        }

        [TestMethod]
        public async Task ReturnsNotFoundResultIfNoSpecificationLocatedWithTheSuppliedIdForProvidersApproval()
        {
            GivenTheApiResponseDetailsForTheSuppliedId(null, HttpStatusCode.NotFound);

            await WhenBatchProvidersAreApproved();

            ThenTheResponseShouldBe<NotFoundResult>();
        }

        [TestMethod]
        public async Task ReturnsPreConditionFailedResultIfNotSelectedForFundingForSuppliedSpecificationIdForSpecificationApproval()
        {
            ApiSpecificationSummary specificationSummary = NewApiSpecificationSummary(_ =>
                _.WithIsSelectedForFunding(false));

            string approveFundingJobId = NewRandomString();
            ApiJob approveFundingJob = NewJob(_ => _.WithId(approveFundingJobId));

            GivenTheApiResponseDetailsForTheSuppliedId(specificationSummary);
            AndTheApiResponseDetailsForApproveSpecificationJob(approveFundingJob);

            await WhenAllProvidersAreApproved();

            ThenTheResponseShouldBe<PreconditionFailedResult>();
        }

        [TestMethod]
        public async Task ReturnsPreConditionFailedResultIfNotSelectedForFundingForSuppliedSpecificationIdForProvidersApproval()
        {
            ApiSpecificationSummary specificationSummary = NewApiSpecificationSummary(_ =>
                _.WithIsSelectedForFunding(false));

            string approveFundingJobId = NewRandomString();
            ApiJob approveFundingJob = NewJob(_ => _.WithId(approveFundingJobId));

            GivenTheApiResponseDetailsForTheSuppliedId(specificationSummary);
            AndTheApiResponseDetailsForApproveSpecificationJob(approveFundingJob);

            await WhenBatchProvidersAreApproved();

            ThenTheResponseShouldBe<PreconditionFailedResult>();
        }

        [TestMethod]
        public async Task ReturnsStatusCode412IfTheFundingConfigurationDoesNotAllowAllApprovaModeForPublishAllProvidersFunding()
        {
            GivenTheApiResponseDetailsForTheSuppliedId(NewApiSpecificationSummary(_ =>
                _.WithIsSelectedForFunding(true)
                .WithId(SpecificationId)));
            AndTheFundingConfigurationsForSpecificationSummary(NewApiFundingConfiguration(_ => _.WithApprovalMode(ApprovalMode.Batches)));

            await WhenAllProvidersAreApproved();

            ThenTheResponseShouldBe<PreconditionFailedResult>(_ =>
                _.Value.Equals($"Specification with id : {SpecificationId} has funding configurations which does not match required approval mode={ApprovalMode.All}"));
        }

        [TestMethod]
        public async Task ReturnsStatusCode412IfTheFundingConfigurationDoesNotAllowBatchesApprovaModeForPublishBatchProvidersFunding()
        {
            GivenTheApiResponseDetailsForTheSuppliedId(NewApiSpecificationSummary(_ =>
                _.WithIsSelectedForFunding(true)
                .WithId(SpecificationId)));
            AndTheFundingConfigurationsForSpecificationSummary(NewApiFundingConfiguration(_ => _.WithApprovalMode(ApprovalMode.All)));

            await WhenBatchProvidersAreApproved();

            ThenTheResponseShouldBe<PreconditionFailedResult>(_ =>
                _.Value.Equals($"Specification with id : {SpecificationId} has funding configurations which does not match required approval mode={ApprovalMode.Batches}"));
        }

        [TestMethod]
        public async Task ReturnsInternalServerErrorResultIfJobNotCreatedForSuppliedSpecificationIdForSpecificationApproval()
        {
            ApiSpecificationSummary specificationSummary = NewApiSpecificationSummary(_ =>
                _.WithIsSelectedForFunding(true));

            string approveFundingJobId = NewRandomString();

            GivenTheApiResponseDetailsForTheSuppliedId(specificationSummary);

            await WhenAllProvidersAreApproved();

            ThenTheResponseShouldBe<InternalServerErrorResult>();
        }

        [TestMethod]
        public async Task ReturnsInternalServerErrorResultIfJobNotCreatedForSuppliedSpecificationIdForProvidersApproval()
        {
            ApiSpecificationSummary specificationSummary = NewApiSpecificationSummary(_ =>
                _.WithIsSelectedForFunding(true));

            string approveFundingJobId = NewRandomString();

            GivenTheApiResponseDetailsForTheSuppliedId(specificationSummary);

            await WhenBatchProvidersAreApproved();

            ThenTheResponseShouldBe<InternalServerErrorResult>();
        }

        [TestMethod]
        public async Task ApproveAllProvidersFundingJobForSuppliedSpecificationId()
        {
            ApiSpecificationSummary specificationSummary = NewApiSpecificationSummary(_ =>
                _.WithIsSelectedForFunding(true));

            string approveFundingJobId = NewRandomString();
            ApiJob approveFundingJob = NewJob(_ => _.WithId(approveFundingJobId));

            GivenTheApiResponseDetailsForTheSuppliedId(specificationSummary);
            AndTheApiResponseDetailsForApproveSpecificationJob(approveFundingJob);

            await WhenAllProvidersAreApproved();

            JobCreationResponse expectedJobCreationResponse = NewJobCreationResponse(_ => _.WithJobId(approveFundingJobId));

            ActionResult
                .Should()
                .BeOfType<OkObjectResult>()
                .Which
                .Value
                .Should()
                .BeEquivalentTo(expectedJobCreationResponse);
        }

        [TestMethod]
        public async Task ApproveProvidersFundingJobForSuppliedSpecificationId()
        {
            ApiSpecificationSummary specificationSummary = NewApiSpecificationSummary(_ =>
                _.WithIsSelectedForFunding(true));

            string approveFundingJobId = NewRandomString();
            ApiJob approveFundingJob = NewJob(_ => _.WithId(approveFundingJobId));

            Dictionary<string, string> messageProperties = new Dictionary<string, string>
            {
                {JobConstants.MessagePropertyNames.PublishedProviderIdsRequest, JsonExtensions.AsJson(_approveProvidersRequest) }
            };

            GivenTheApiResponseDetailsForTheSuppliedId(specificationSummary);
            AndTheApiResponseDetailsForApproveProviderJob(approveFundingJob, messageProperties);

            await WhenBatchProvidersAreApproved();

            JobCreationResponse expectedJobCreationResponse = NewJobCreationResponse(_ => _.WithJobId(approveFundingJobId));

            ActionResult
                .Should()
                .BeOfType<OkObjectResult>()
                .Which
                .Value
                .Should()
                .BeEquivalentTo(expectedJobCreationResponse);
        }

        [TestMethod]
        public async Task CanChooseForFunding_SpecificationSummaryIsNull_ReturnsNotFound()
        {
            await WhenCanChooseFundingIsQueried();
            
            ActionResult
                .Should()
                .BeAssignableTo<NotFoundResult>();
        }

        [TestMethod]
        [DataRow(SpecificationFundingStatus.CanChoose)]
        [DataRow(SpecificationFundingStatus.AlreadyChosen)]
        [DataRow(SpecificationFundingStatus.SharesAlreadyChosenFundingStream)]
        public async Task CanChooseForFunding_SpecificationSummaryStatus_ReturnsOKWithCorrectStatus(SpecificationFundingStatus status)
        {
            ApiSpecificationSummary specificationSummary = NewApiSpecificationSummary();

            GivenTheApiResponseDetailsForTheSuppliedId(specificationSummary, HttpStatusCode.OK);
            AndTheSpecificationHasTheStatus(specificationSummary, status);

            await WhenCanChooseFundingIsQueried();

            ActionResult
                .Should()
                .BeAssignableTo<OkObjectResult>()
                .Which
                .Value
                .Should()
                .BeEquivalentTo(new SpecificationCheckChooseForFundingResult { Status = status });
        }

        private void AndTheSpecificationSummaryConflictsWithAnotherForThatFundingPeriod(ApiSpecificationSummary specificationSummary)
        {
            _specificationFundingStatusService
                .CheckChooseForFundingStatus(Arg.Is(specificationSummary))
                .Returns(SpecificationFundingStatus.SharesAlreadyChosenFundingStream);
        }

        private void AndGetPreReqCheckerLocatorReturnsRefreshPreReqChecker()
        {
            _prerequisiteCheckerLocator
                .GetPreReqChecker(PrerequisiteCheckerType.Refresh)
                .Returns(_prerequisiteChecker);
        }

        private void AndPrereqChecksFails(
            ApiSpecificationSummary specificationSummary)
        {
            _prerequisiteChecker
                .PerformChecks(
                    Arg.Is(specificationSummary),
                    null,
                    Arg.Is<IEnumerable<PublishedProvider>>(_ => !_.Any()),
                    Arg.Is<IEnumerable<Provider>>(_ => !_.Any()))
                .Throws(new NonRetriableException());
        }

        private void AndTheSpecificationHasTheStatus(ApiSpecificationSummary specificationSummary,
            SpecificationFundingStatus status)
        {
            _specificationFundingStatusService
                .CheckChooseForFundingStatus(specificationSummary)
                .Returns(status);   
        }

        private async Task WhenCanChooseFundingIsQueried()
        {
            ActionResult = await _service.CanChooseForFunding(SpecificationId);
        }

        private async Task WhenTheSpecificationIsPublished()
        {
            ActionResult = await _service.CreateRefreshFundingJob(SpecificationId, User, CorrelationId);
        }

        private void AndTheApiResponseDetailsForApproveSpecificationJob(ApiJob job)
        {
            _approveSpecificationFundingJobs.CreateJob(SpecificationId, User, CorrelationId, null, null)
                .Returns(job);
        }

        private void AndTheApiResponseDetailsForApproveProviderJob(ApiJob job, Dictionary<string, string> messageProperties)
        {
            _ = _approveProviderFundingJobs.CreateJob(SpecificationId, User, CorrelationId, Arg.Is<Dictionary<string, string>>(_ => _.SequenceEqual(messageProperties)), null)
                .Returns(job);
        }

        private async Task WhenAllProvidersAreApproved()
        {
            ActionResult = await _service.ApproveAllProviderFunding(SpecificationId, User, CorrelationId);
        }
        private async Task WhenBatchProvidersAreApproved()
        {
            ActionResult = await _service.ApproveBatchProviderFunding(SpecificationId, _approveProvidersRequest, User, CorrelationId);
        }

        private void AndTheApiResponseDetailsForTheFundingPeriodId(string fundingPeriodId,
            params ApiSpecificationSummary[] specificationSummaries)
        {
            Specifications.GetSpecificationsSelectedForFundingByPeriod(fundingPeriodId)
                .Returns(new ApiResponse<IEnumerable<ApiSpecificationSummary>>(HttpStatusCode.OK, specificationSummaries));
        }

        private PublishedProviderIdsRequest BuildApproveProvidersRequest(Action<PublishedProviderIdsRequestBuilder> setUp = null)
        {
            PublishedProviderIdsRequestBuilder approveProvidersRequestBuilder = new PublishedProviderIdsRequestBuilder();

            setUp?.Invoke(approveProvidersRequestBuilder);

            return approveProvidersRequestBuilder.Build();
        }
    }
}