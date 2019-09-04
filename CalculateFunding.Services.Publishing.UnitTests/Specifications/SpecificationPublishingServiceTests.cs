using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Specifications;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using ApiSpecificationSummary = CalculateFunding.Common.ApiClient.Specifications.Models.SpecificationSummary;
using ApiJob = CalculateFunding.Common.ApiClient.Jobs.Models.Job;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System.IO;
using System.Text;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Common.Caching;
using FluentAssertions;
using System.Linq;
using CalculateFunding.Models.Publishing;

namespace CalculateFunding.Services.Publishing.UnitTests.Specifications
{
    [TestClass]
    public class SpecificationPublishingServiceTests : SpecificationPublishingServiceTestsBase<RefreshFundingJobDefinition>
    {
        private SpecificationPublishingService _service;
        private ICreateJobsForSpecifications<ApproveFundingJobDefinition> _approvalJobs;
        private ICacheProvider _cacheProvider;
        private ISpecificationFundingStatusService _specificationFundingStatusService;
        private bool _cacheCalled;

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
            string[] expectedErrors = {NewRandomString(), NewRandomString()};

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
            AndTheApiResponseDetailsForSpecificationsJob(refreshFundingJob);

            await WhenTheSpecificationIsPublished();

            ThenTheResponseShouldBe<CreatedResult>(_ => _.Location == $"api/jobs/{refreshFundingJobId}" &&
                                                        ReferenceEquals(_.Value, refreshFundingJob));
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
            AndTheProvidersShouldBeCached();

            await WhenTheSpecificationIsApproved();

            _cacheCalled
                .Should()
                .Be(true);

            ThenTheResponseShouldBe<AcceptedAtActionResult>(_ => _.RouteValues["specificationId"].ToString() == SpecificationId && ReferenceEquals(_.Value, approveFundingJob));
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
        [DataRow(SpecificationFundingStatus.SharesAlreadyChoseFundingStream)]
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

        private void AndTheProvidersShouldBeCached()
        {
            _cacheProvider.When(x => x.CreateListAsync<string>(Arg.Is<IEnumerable<string>>(providers => providers.SequenceEqual(CreateApprovalModel.Providers)), Arg.Any<string>()))
                .Do(y => _cacheCalled = true);
        }

        private void AndTheApiResponseDetailsForApprovalJob(ApiJob job)
        {
            _approvalJobs.CreateJob(SpecificationId, User, CorrelationId, Arg.Is<Dictionary<string, string>>(x => x["fundingStreamId"] == CreateApprovalModel.FundingStreamId) , JsonConvert.SerializeObject(CreateApprovalModel))
                .Returns(job);
        }

        private async Task WhenTheSpecificationIsApproved()
        {
            HttpRequest request = Substitute.For<HttpRequest>();

            
            byte[] byteArray = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(CreateApprovalModel));
            MemoryStream stream = new MemoryStream(byteArray);

            request.Body
                .Returns(stream);

            ActionResult = await _service.ApproveSpecification("", "", SpecificationId, request, User, CorrelationId);
        }

        private void AndTheApiResponseDetailsForTheFundingPeriodId(string fundingPeriodId,
            params ApiSpecificationSummary[] specificationSummaries)
        {
            Specifications.GetSpecificationsSelectedForFundingByPeriod(fundingPeriodId)
                .Returns(new ApiResponse<IEnumerable<ApiSpecificationSummary>>(HttpStatusCode.OK, specificationSummaries));
        }
    }
}