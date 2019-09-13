using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Specifications;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace CalculateFunding.Services.Publishing.UnitTests.Specifications
{
    [TestClass]
    public class PublishedProviderFundingServiceTests
    {
        private IPublishedFundingRepository _publishedFunding;
        private ISpecificationIdServiceRequestValidator _validator;
        private ValidationResult _validationResult;

        private PublishedProviderFundingService _service;

        private string _specificationId;
        private IActionResult _actionResult;

        [TestInitialize]
        public void SetUp()
        {
            _specificationId = NewRandomString();
            _validationResult = new ValidationResult();
            
            _validator = Substitute.For<ISpecificationIdServiceRequestValidator>();
            _validator.Validate(_specificationId)
                .Returns(_validationResult);
            
            _publishedFunding = Substitute.For<IPublishedFundingRepository>();

            _service = new PublishedProviderFundingService(new ResiliencePolicies
                {
                    PublishedFundingRepository = Polly.Policy.NoOpAsync()
                },
                _publishedFunding,
                _validator);
        }

        [TestMethod]
        public async Task ReturnsBadRequestWhenSuppliedSpecificationIdFailsValidation()
        {
            string[] expectedErrors = {NewRandomString(), NewRandomString()};

            GivenTheValidationErrors(expectedErrors);

            await WhenThePublishedProvidersAreQueried();

            ThenTheResponseShouldBe<BadRequestObjectResult>();
        }

        [TestMethod]
        public async Task ReturnsTheCurrentPublishedProviderVersionFromEachPublishedProviderTheRepositoryFinds()
        {
            PublishedProviderVersion firstExpectedVersion = NewPublishedProviderVersion();
            PublishedProviderVersion secondExpectedVersion = NewPublishedProviderVersion();
            PublishedProviderVersion thirdExpectedVersion = NewPublishedProviderVersion();

            GivenThePublishedProvidersForTheSpecificationId(
                NewPublishedProvider(_ => _.WithCurrent(firstExpectedVersion)),
                NewPublishedProvider(_ => _.WithCurrent(secondExpectedVersion)),
                NewPublishedProvider(_ => _.WithCurrent(thirdExpectedVersion)));

            await WhenThePublishedProvidersAreQueried();

            ThenTheResponseShouldBe<OkObjectResult>(_ =>
                ((IEnumerable<PublishedProviderVersion>) _.Value).SequenceEqual(new[]
                {
                    firstExpectedVersion,
                    secondExpectedVersion,
                    thirdExpectedVersion
                }));
        }
        
        [TestMethod]
        public async Task HealthCheckCollectsStatusFromRepository()
        {
            DependencyHealth firstExpectedDependency = new DependencyHealth();
            DependencyHealth secondExpectedDependency = new DependencyHealth();
            DependencyHealth thirdExpectedDependency = new DependencyHealth();

            GivenTheRepositoryServiceHealth(firstExpectedDependency,
                secondExpectedDependency,
                thirdExpectedDependency);

            ServiceHealth isHealthOk = await _service.IsHealthOk();

            isHealthOk
                .Should()
                .NotBeNull();

            isHealthOk
                .Name
                .Should()
                .Be(nameof(PublishedProviderFundingService));

            isHealthOk
                .Dependencies
                .Should()
                .BeEquivalentTo(firstExpectedDependency, secondExpectedDependency, thirdExpectedDependency);
        }

        private async Task WhenThePublishedProvidersAreQueried()
        {
            _actionResult = await _service.GetLatestPublishedProvidersForSpecificationId(_specificationId);
        }

        private string NewRandomString()
        {
            return new RandomString();
        }

        private void GivenTheValidationErrors(params string[] errors)
        {
            foreach (var error in errors) _validationResult.Errors.Add(new ValidationFailure(error, error));
        }

        private void ThenTheResponseShouldBe<TActionResult>(Expression<Func<TActionResult, bool>> matcher = null)
            where TActionResult : IActionResult
        {
            _actionResult
                .Should()
                .BeOfType<TActionResult>();

            if (matcher == null) return;

            ((TActionResult) _actionResult)
                .Should()
                .Match(matcher);
        }

        private void GivenThePublishedProvidersForTheSpecificationId(params PublishedProvider[] publishedProviders)
        {
            _publishedFunding.GetLatestPublishedProvidersBySpecification(_specificationId)
                .Returns(publishedProviders);
        }

        private PublishedProvider NewPublishedProvider(Action<PublishedProviderBuilder> setUp = null)
        {
            PublishedProviderBuilder builder = new PublishedProviderBuilder();

            setUp?.Invoke(builder);

            return builder.Build();
        }

        private PublishedProviderVersion NewPublishedProviderVersion(
            Action<PublishedProviderVersionBuilder> setUp = null)
        {
            PublishedProviderVersionBuilder builder = new PublishedProviderVersionBuilder();

            setUp?.Invoke(builder);

            return builder.Build();
        }
        
        private void GivenTheRepositoryServiceHealth(params DependencyHealth[] dependencies)
        {
            ServiceHealth serviceHealth = new ServiceHealth();

            serviceHealth.Dependencies.AddRange(dependencies);

            _publishedFunding.IsHealthOk().Returns(serviceHealth);
        }
    }
}