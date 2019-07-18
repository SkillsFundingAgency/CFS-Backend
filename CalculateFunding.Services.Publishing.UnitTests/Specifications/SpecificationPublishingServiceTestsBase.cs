using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Net;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Specifications;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.Models;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Specifications;
using FluentAssertions;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using NSubstitute;
using Polly;
using SpecModels = CalculateFunding.Models.Specs;

namespace CalculateFunding.Services.Publishing.UnitTests.Specifications
{
    public abstract class SpecificationPublishingServiceTestsBase<TJobDefinition>
        where TJobDefinition : IJobDefinition
    {
        protected ISpecificationsApiClient Specifications { get; private set; }
        protected ICreateJobsForSpecifications<TJobDefinition> Jobs { get; private set; }
        protected ValidationResult ValidationResult { get; private set; }
        protected IActionResult ActionResult { get; set; }
        protected string SpecificationId { get; private set; }
        protected string CorrelationId { get; private set; }
        protected Reference User { get; private set; }
        protected IPublishSpecificationValidator Validator { get; private set; }
        protected ResiliencePolicies ResiliencePolicies { get; private set; }
        protected SpecModels.SpecificationApprovalModel CreateApprovalModel { get; private set; }

        [TestInitialize]
        public void TestBaseSetUp()
        {
            ValidationResult = new ValidationResult();
            SpecificationId = NewRandomString();
            CorrelationId = NewRandomString();
            User = NewUser();
            Jobs = Substitute.For<ICreateJobsForSpecifications<TJobDefinition>>();

            CreateApprovalModel = new SpecModels.SpecificationApprovalModel { FundingStreamId = NewRandomString(), Providers = new List<string> { NewRandomString(), NewRandomString() } };

            Validator = Substitute.For<IPublishSpecificationValidator>();

            Validator.Validate(SpecificationId)
                .Returns(ValidationResult);

            Specifications = Substitute.For<ISpecificationsApiClient>();

            ResiliencePolicies = new ResiliencePolicies
            {
                SpecificationsRepositoryPolicy = Policy.NoOpAsync()
            };
        }

        protected string NewRandomString()
        {
            return new RandomString();
        }

        protected Reference NewUser(Action<UserBuilder> setUp = null)
        {
            UserBuilder userBuilder = new UserBuilder();

            setUp?.Invoke(userBuilder);

            return userBuilder.Build();
        }

        protected Job NewJob(Action<JobBuilder> setUp = null)
        {
            JobBuilder jobBuilder = new JobBuilder();

            setUp?.Invoke(jobBuilder);

            return jobBuilder.Build();
        }

        protected void GivenTheApiResponseDetailsForTheSuppliedId(SpecificationSummary specificationSummary,
            HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            Specifications.GetSpecificationSummaryById(SpecificationId)
                .Returns(new ApiResponse<SpecificationSummary>(statusCode,
                    specificationSummary));
        }

        protected SpecificationSummary NewApiSpecificationSummary(Action<SpecificationSummaryBuilder> setUp = null)
        {
            SpecificationSummaryBuilder builder = new SpecificationSummaryBuilder();

            setUp?.Invoke(builder);

            return builder.Build();
        }

        protected void GivenTheValidationErrors(params string[] errors)
        {
            foreach (string error in errors)
            {
                ValidationResult.Errors.Add(new ValidationFailure(error, error));
            }
        }

        protected void ThenTheResponseShouldBe<TActionResult>(Expression<Func<TActionResult, bool>> matcher = null)
            where TActionResult : IActionResult
        {
            ActionResult
                .Should()
                .BeOfType<TActionResult>();

            if (matcher == null)
            {
                return;
            }

            ((TActionResult) ActionResult)
                .Should()
                .Match(matcher);
        }

        protected void AndTheApiResponseDetailsForSpecificationsJob(Job job)
        {
            Jobs.CreateJob(SpecificationId, User, CorrelationId)
                .Returns(job);
        }
    }
}