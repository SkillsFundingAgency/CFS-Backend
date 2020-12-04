using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Policies.Models.FundingConfig;
using CalculateFunding.Common.ApiClient.Specifications;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace CalculateFunding.Services.Publishing.UnitTests.Specifications
{
    public abstract class SpecificationPublishingServiceTestsBase<TJobCreation>
        where TJobCreation : class, ICreateJobsForSpecifications
    {
        private ValidationResult _validationResult;

        protected ISpecificationsApiClient Specifications { get; private set; }

        protected IProviderService ProviderService { get; private set; }

        protected TJobCreation Jobs { get; private set; }
        protected IActionResult ActionResult { get; set; }
        protected string SpecificationId { get; private set; }
        protected string[] ProviderIds { get; private set; }
        protected string CorrelationId { get; private set; }
        protected string FundingStreamId { get; private set; }
        protected string FundingPeriodId { get; private set; }
        protected Reference User { get; private set; }
        protected ISpecificationIdServiceRequestValidator SpecificationIdValidator { get; private set; }
        protected IPublishedProviderIdsServiceRequestValidator ProviderIdsValidator { get; private set; }
        protected ResiliencePolicies ResiliencePolicies { get; private set; }
        protected IFundingConfigurationService FundingConfigurationService { get; private set; }

        [TestInitialize]
        public void TestBaseSetUp()
        {
            _validationResult = new ValidationResult();
            
            SpecificationId = NewRandomString();
            string providerId = NewRandomString();
            ProviderIds = new[] { providerId };

            FundingStreamId = NewRandomString();
            FundingPeriodId = NewRandomString();
            CorrelationId = NewRandomString();
            User = NewUser();
            Jobs = Substitute.For<TJobCreation>();
            SpecificationIdValidator = Substitute.For<ISpecificationIdServiceRequestValidator>();
            ProviderIdsValidator = Substitute.For<IPublishedProviderIdsServiceRequestValidator>();

            SpecificationIdValidator.Validate(SpecificationId)
                .Returns(_validationResult);
            ProviderIdsValidator.Validate(Arg.Is<string[]>(_=>_.SequenceEqual(ProviderIds)) )
                .Returns(_validationResult);

            Specifications = Substitute.For<ISpecificationsApiClient>();
            ProviderService = Substitute.For<IProviderService>();

            ResiliencePolicies = new ResiliencePolicies
            {
                SpecificationsRepositoryPolicy = Polly.Policy.NoOpAsync(),
                PublishedFundingRepository = Polly.Policy.NoOpAsync()
            };

            FundingConfigurationService = Substitute.For<IFundingConfigurationService>();
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

        protected JobCreationResponse NewJobCreationResponse(Action<JobCreationResponseBuilder> setUp = null)
        {
            JobCreationResponseBuilder jobBuilder = new JobCreationResponseBuilder();

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

        protected void GivenTheApiResponseProviderDetailsForSuppliedId(IEnumerable<string> providers)
        {
            ProviderService.GetScopedProviderIdsForSpecification(SpecificationId)
                .Returns(providers);
        }

        protected void AndTheFundingConfigurationsForSpecificationSummary(FundingConfiguration fundingConfiguration)
        {
            FundingConfigurationService
                .GetFundingConfigurations(Arg.Is<SpecificationSummary>(_ => _.Id == SpecificationId))
                .Returns(new Dictionary<string, FundingConfiguration>
                {
                    { NewRandomString(), fundingConfiguration }
                });
        }

        protected SpecificationSummary NewApiSpecificationSummary(Action<SpecificationSummaryBuilder> setUp = null)
        {
            SpecificationSummaryBuilder builder = new SpecificationSummaryBuilder();

            setUp?.Invoke(builder);

            return builder.Build();
        }

        protected FundingConfiguration NewApiFundingConfiguration(Action<FundingConfigurationBuilder> setUp = null)
        {
            FundingConfigurationBuilder builder = new FundingConfigurationBuilder();

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