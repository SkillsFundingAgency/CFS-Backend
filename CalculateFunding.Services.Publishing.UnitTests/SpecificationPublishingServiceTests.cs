using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Specifications;
using CalculateFunding.Common.Models;
using CalculateFunding.Services.Calcs;
using CalculateFunding.Services.Publishing.Interfaces;
using FluentAssertions;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Polly;
using ApiSpecificationSummary = CalculateFunding.Common.ApiClient.Specifications.Models.SpecificationSummary;
using ApiJob = CalculateFunding.Common.ApiClient.Jobs.Models.Job;

namespace CalculateFunding.Services.Publishing.UnitTests
{
    [TestClass]
    public class SpecificationPublishingServiceTests : SpecificationPublishingServiceTestsBase
    {
        private ISpecificationsApiClient _specifications;
        private ICreateRefreshFundingJobs _jobs;
        private SpecificationPublishingService _service;
        private ValidationResult _validationResult;
        private IActionResult _actionResult;

        private string _specificationId;
        private string _correlationId;
        private Reference _user;

        [TestInitialize]
        public void SetUp()
        {
            _validationResult = new ValidationResult();
            _specificationId = NewRandomString();
            _correlationId = NewRandomString();
            _user = NewUser();

            IPublishSpecificationValidator validator = Substitute.For<IPublishSpecificationValidator>();

            validator.Validate(_specificationId)
                .Returns(_validationResult);

            _specifications = Substitute.For<ISpecificationsApiClient>();
            _jobs = Substitute.For<ICreateRefreshFundingJobs>();

            _service = new SpecificationPublishingService(validator,
                _specifications,
                new CalculateFunding.Services.Calcs.ResiliencePolicies
                {
                    SpecificationsRepositoryPolicy = Policy.NoOpAsync()
                },
                _jobs);
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
            AndTheApiResponseDetailsForTheRefreshFundingJob(refreshFundingJob);

            await WhenTheSpecificationIsPublished();

            ThenTheResponseShouldBe<CreatedResult>(_ => _.Location == $"api/jobs/{refreshFundingJobId}" &&
                                                        ReferenceEquals(_.Value, refreshFundingJob));
        }

        private void ThenTheResponseShouldBe<TActionResult>(Expression<Func<TActionResult, bool>> matcher = null)
            where TActionResult : IActionResult
        {
            _actionResult
                .Should()
                .BeOfType<TActionResult>();

            if (matcher == null)
            {
                return;
            }

            ((TActionResult) _actionResult)
                .Should()
                .Match(matcher);
        }

        private async Task WhenTheSpecificationIsPublished()
        {
            _actionResult = await _service.CreatePublishJob(_specificationId, _user, _correlationId);
        }

        private void GivenTheValidationErrors(params string[] errors)
        {
            foreach (string error in errors)
            {
                _validationResult.Errors.Add(new ValidationFailure(error, error));
            }
        }

        private void GivenTheApiResponseDetailsForTheSuppliedId(ApiSpecificationSummary specificationSummary,
            HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            _specifications.GetSpecificationSummaryById(_specificationId)
                .Returns(new ApiResponse<ApiSpecificationSummary>(statusCode,
                    specificationSummary));
        }

        private void AndTheApiResponseDetailsForTheFundingPeriodId(string fundingPeriodId,
            params ApiSpecificationSummary[] specificationSummaries)
        {
            _specifications.GetSpecificationsSelectedForFundingByPeriod(fundingPeriodId)
                .Returns(new ApiResponse<IEnumerable<ApiSpecificationSummary>>(HttpStatusCode.OK, specificationSummaries));
        }

        private void AndTheApiResponseDetailsForTheRefreshFundingJob(ApiJob job)
        {
            _jobs.CreateJob(_specificationId, _user, _correlationId)
                .Returns(job);
        }

        private ApiSpecificationSummary NewApiSpecificationSummary(Action<SpecificationSummaryBuilder> setUp = null)
        {
            SpecificationSummaryBuilder builder = new SpecificationSummaryBuilder();

            setUp?.Invoke(builder);

            return builder.Build();
        }
    }
}