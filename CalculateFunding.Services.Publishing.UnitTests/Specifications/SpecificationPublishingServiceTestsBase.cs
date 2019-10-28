using System;
using System.Linq.Expressions;
using System.Net;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Specifications;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.Models;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace CalculateFunding.Services.Publishing.UnitTests.Specifications
{
    public abstract class SpecificationPublishingServiceTestsBase<TJobDefinition>
        where TJobDefinition : IJobDefinition
    {
        private ValidationResult _validationResult;

        protected ISpecificationsApiClient Specifications { get; private set; }
        protected ICreateJobsForSpecifications<TJobDefinition> Jobs { get; private set; }
        protected IActionResult ActionResult { get; set; }
        protected string SpecificationId { get; private set; }
        protected string CorrelationId { get; private set; }
        protected Reference User { get; private set; }
        protected ISpecificationIdServiceRequestValidator Validator { get; private set; }
        protected ResiliencePolicies ResiliencePolicies { get; private set; }

        [TestInitialize]
        public void TestBaseSetUp()
        {
            _validationResult = new ValidationResult();
            SpecificationId = NewRandomString();
            CorrelationId = NewRandomString();
            User = NewUser();
            Jobs = Substitute.For<ICreateJobsForSpecifications<TJobDefinition>>();

            Validator = Substitute.For<ISpecificationIdServiceRequestValidator>();

            Validator.Validate(SpecificationId)
                .Returns(_validationResult);

            Specifications = Substitute.For<ISpecificationsApiClient>();

            ResiliencePolicies = new ResiliencePolicies
            {
                SpecificationsRepositoryPolicy = Polly.Policy.NoOpAsync()
            };
        }

        protected string NewRandomString()
        {
            return new RandomString();
        }

        private Reference NewUser(Action<UserBuilder> setUp = null)
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
            foreach (var error in errors) _validationResult.Errors.Add(new ValidationFailure(error, error));
        }

        protected void ThenTheResponseShouldBe<TActionResult>(Expression<Func<TActionResult, bool>> matcher = null)
            where TActionResult : IActionResult
        {
            ActionResult
                .Should()
                .BeOfType<TActionResult>();

            if (matcher == null) return;

            ((TActionResult)ActionResult)
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