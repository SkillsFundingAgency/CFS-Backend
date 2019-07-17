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

namespace CalculateFunding.Services.Publishing.UnitTests.Specifications
{
    [TestClass]
    public class SpecificationPublishingServiceTests : SpecificationPublishingServiceTestsBase<ICreateRefreshFundingJobs>
    {
        private SpecificationPublishingService _service;

        [TestInitialize]
        public void SetUp()
        {
            Jobs = Substitute.For<ICreateRefreshFundingJobs>();

            _service = new SpecificationPublishingService(Validator,
                Specifications,
                ResiliencePolicies,
                Jobs);
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

        private async Task WhenTheSpecificationIsPublished()
        {
            ActionResult = await _service.CreatePublishJob(SpecificationId, User, CorrelationId);
        }

        private void AndTheApiResponseDetailsForTheFundingPeriodId(string fundingPeriodId,
            params ApiSpecificationSummary[] specificationSummaries)
        {
            Specifications.GetSpecificationsSelectedForFundingByPeriod(fundingPeriodId)
                .Returns(new ApiResponse<IEnumerable<ApiSpecificationSummary>>(HttpStatusCode.OK, specificationSummaries));
        }
    }
}