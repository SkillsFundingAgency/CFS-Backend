using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.Caching;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Specifications;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using ApiJob = CalculateFunding.Common.ApiClient.Jobs.Models.Job;
using ApiSpecificationSummary = CalculateFunding.Common.ApiClient.Specifications.Models.SpecificationSummary;

namespace CalculateFunding.Services.Publishing.UnitTests.Specifications
{
    [TestClass]
    public class SpecificationPublishingServiceTests : SpecificationPublishingServiceTestsBase<RefreshFundingJobDefinition>
    {
        private SpecificationPublishingService _service;
        private ICreateJobsForSpecifications<ApproveFundingJobDefinition> _approvalJobs;
        private ICacheProvider _cacheProvider;
        private ISpecificationFundingStatusService _specificationFundingStatusService;

        [TestInitialize]
        public void SetUp()
        {
            _approvalJobs = Substitute.For<ICreateJobsForSpecifications<ApproveFundingJobDefinition>>();
            _cacheProvider = Substitute.For<ICacheProvider>();
            _specificationFundingStatusService = Substitute.For<ISpecificationFundingStatusService>();

            _service = new SpecificationPublishingService(Validator,
                Specifications,
                ResiliencePolicies,
                _cacheProvider,
                Jobs,
                _approvalJobs,
                _specificationFundingStatusService);
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

        private void AndTheSpecificationSummaryConflictsWithAnotherForThatFundingPeriod(ApiSpecificationSummary specificationSummary)
        {
            _specificationFundingStatusService
                .CheckChooseForFundingStatus(Arg.Is(specificationSummary))
                .Returns(SpecificationFundingStatus.SharesAlreadyChosenFundingStream);
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
        public async Task ReturnsBadRequestWhenSuppliedSpecificationIdFailsValidationForApproval()
        {
            string[] expectedErrors = { NewRandomString(), NewRandomString() };

            GivenTheValidationErrors(expectedErrors);

            await WhenTheSpecificationIsApproved();

            ThenTheResponseShouldBe<BadRequestObjectResult>();
        }

        [TestMethod]
        public async Task ReturnsNotFoundResultIfNoSpecificationLocatedWithTheSuppliedIdForApproval()
        {
            GivenTheApiResponseDetailsForTheSuppliedId(null, HttpStatusCode.NotFound);

            await WhenTheSpecificationIsApproved();

            ThenTheResponseShouldBe<NotFoundResult>();
        }

        [TestMethod]
        public async Task ReturnsPreConditionFailedResultIfNotSelectedForFundingForSuppliedSpecificationId()
        {
            ApiSpecificationSummary specificationSummary = NewApiSpecificationSummary(_ =>
                _.WithIsSelectedForFunding(false));

            string approveFundingJobId = NewRandomString();
            ApiJob approveFundingJob = NewJob(_ => _.WithId(approveFundingJobId));

            GivenTheApiResponseDetailsForTheSuppliedId(specificationSummary);
            AndTheApiResponseDetailsForApprovalJob(approveFundingJob);

            await WhenTheSpecificationIsApproved();

            ThenTheResponseShouldBe<PreconditionFailedResult>();
        }

        [TestMethod]
        public async Task ReturnsInternalServerErrorResultIfJobNotCreatedForSuppliedSpecificationId()
        {
            ApiSpecificationSummary specificationSummary = NewApiSpecificationSummary(_ =>
                _.WithIsSelectedForFunding(true));

            string approveFundingJobId = NewRandomString();

            GivenTheApiResponseDetailsForTheSuppliedId(specificationSummary);

            await WhenTheSpecificationIsApproved();

            ThenTheResponseShouldBe<InternalServerErrorResult>();
        }

        [TestMethod]
        public async Task ApproveFundingJobForSuppliedSpecificationId()
        {
            ApiSpecificationSummary specificationSummary = NewApiSpecificationSummary(_ =>
                _.WithIsSelectedForFunding(true));

            string approveFundingJobId = NewRandomString();
            ApiJob approveFundingJob = NewJob(_ => _.WithId(approveFundingJobId));

            GivenTheApiResponseDetailsForTheSuppliedId(specificationSummary);
            AndTheApiResponseDetailsForApprovalJob(approveFundingJob);

            await WhenTheSpecificationIsApproved();

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
            //Arrange
            GivenTheApiResponseDetailsForTheSuppliedId(null, HttpStatusCode.NotFound);

            //Act
            IActionResult actionResult = await _service.CanChooseForFunding(SpecificationId);

            //Assert
            actionResult
                .Should()
                .BeAssignableTo<NotFoundResult>();
        }

        [TestMethod]
        [DataRow(SpecificationFundingStatus.CanChoose)]
        [DataRow(SpecificationFundingStatus.AlreadyChosen)]
        [DataRow(SpecificationFundingStatus.SharesAlreadyChosenFundingStream)]
        public async Task CanChooseForFunding_SpecificationSummaryStatus_ReturnsOKWithCorrectStatus(SpecificationFundingStatus status)
        {
            //Arrange
            ApiSpecificationSummary specificationSummary = new ApiSpecificationSummary();

            GivenTheApiResponseDetailsForTheSuppliedId(specificationSummary, HttpStatusCode.OK);

            _specificationFundingStatusService
                .CheckChooseForFundingStatus(Arg.Is(specificationSummary))
                .Returns(status);

            //Act
            IActionResult actionResult = await _service.CanChooseForFunding(SpecificationId);

            //Assert
            actionResult
                .Should()
                .BeAssignableTo<OkObjectResult>()
                .Which
                .Value
                .Should()
                .BeEquivalentTo(new SpecificationCheckChooseForFundingResult { Status = status });
        }

        private async Task WhenTheSpecificationIsPublished()
        {
            ActionResult = await _service.CreateRefreshFundingJob(SpecificationId, User, CorrelationId);
        }

        private void AndTheApiResponseDetailsForApprovalJob(ApiJob job)
        {
            _approvalJobs.CreateJob(SpecificationId, User, CorrelationId, null, null)
                .Returns(job);
        }

        private async Task WhenTheSpecificationIsApproved()
        {
            ActionResult = await _service.ApproveSpecification("", "", SpecificationId, User, CorrelationId);
        }

        private void AndTheApiResponseDetailsForTheFundingPeriodId(string fundingPeriodId,
            params ApiSpecificationSummary[] specificationSummaries)
        {
            Specifications.GetSpecificationsSelectedForFundingByPeriod(fundingPeriodId)
                .Returns(new ApiResponse<IEnumerable<ApiSpecificationSummary>>(HttpStatusCode.OK, specificationSummaries));
        }
    }
}