using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Models.Publishing;
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
        private IPublishedFundingDataService _publishedFunding;
        private ISpecificationService _specificationService;
        private ISpecificationIdServiceRequestValidator _validator;
        private ValidationResult _validationResult;

        private PublishedProviderFundingService _service;

        private string _specificationId;
        private IActionResult _actionResult;
        private string _fundingStreamId;
        private string _fundingPeriodId;
        private SpecificationSummary _specificationSummary;

        [TestInitialize]
        public void SetUp()
        {
            _specificationId = NewRandomString();
            _validationResult = new ValidationResult();
            _fundingPeriodId = NewRandomString();
            _fundingStreamId = NewRandomString();

            _validator = Substitute.For<ISpecificationIdServiceRequestValidator>();
            _validator.Validate(_specificationId)
                .Returns(_validationResult);

            _publishedFunding = Substitute.For<IPublishedFundingDataService>();
            _specificationService = Substitute.For<ISpecificationService>();

            _service = new PublishedProviderFundingService(new ResiliencePolicies
            {
                PublishedFundingRepository = Polly.Policy.NoOpAsync()
            },
                _publishedFunding,
                _specificationService,
                _validator);
        }

        [TestMethod]
        public async Task ReturnsBadRequestWhenSuppliedSpecificationIdFailsValidation()
        {
            string[] expectedErrors = { NewRandomString(), NewRandomString() };

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

            AndTheSpecificationSummaryIsRetrieved(NewSpecificationSummary(s =>
            {
                s.WithId(_specificationId);
                s.WithFundingPeriodId(_fundingPeriodId);
                s.WithFundingStreamIds(new[] { _fundingStreamId });
            }));

            await WhenThePublishedProvidersAreQueried();

            ThenTheResponseShouldBe<OkObjectResult>(_ =>
                ((IEnumerable<PublishedProviderVersion>)_.Value).SequenceEqual(new[]
                {
                    firstExpectedVersion,
                    secondExpectedVersion,
                    thirdExpectedVersion
                }));
        }

        private void AndTheSpecificationSummaryIsRetrieved(SpecificationSummary specificationSummary)
        {
            _specificationSummary = specificationSummary;
            _specificationService
                .GetSpecificationSummaryById(Arg.Is(_specificationId))
                .Returns(_specificationSummary);
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

            ((TActionResult)_actionResult)
                .Should()
                .Match(matcher);
        }

        private void GivenThePublishedProvidersForTheSpecificationId(params PublishedProvider[] publishedProviders)
        {
            _publishedFunding.GetCurrentPublishedProviders(_fundingStreamId, _fundingPeriodId)
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

        private SpecificationSummary NewSpecificationSummary(Action<SpecificationSummaryBuilder> setUp = null)
        {
            SpecificationSummaryBuilder builder = new SpecificationSummaryBuilder();

            setUp?.Invoke(builder);

            return builder.Build();
        }
    }
}