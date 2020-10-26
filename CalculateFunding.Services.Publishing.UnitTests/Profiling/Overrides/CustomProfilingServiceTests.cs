using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Profiling.Custom;
using CalculateFunding.Tests.Common.Builders;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Polly;
using Serilog.Core;

namespace CalculateFunding.Services.Publishing.UnitTests.Profiling.Overrides
{
    [TestClass]
    public class CustomProfilingServiceTests : CustomProfileRequestTestBase
    {
        private CustomProfilingService _service;
        private Mock<IPublishedProviderStatusUpdateService> _publishedProviderVersionCreation;
        private Mock<IValidator<ApplyCustomProfileRequest>> _validator;
        private Mock<IPublishedFundingRepository> _publishedFunding;
        
        [TestInitialize]
        public void SetUp()
        {
                _publishedProviderVersionCreation = new Mock<IPublishedProviderStatusUpdateService>();
                _validator = new Mock<IValidator<ApplyCustomProfileRequest>>();
                _publishedFunding = new Mock<IPublishedFundingRepository>();
                
                _service = new CustomProfilingService(_publishedProviderVersionCreation.Object,
                    _validator.Object,
                    _publishedFunding.Object,
                    new ResiliencePolicies
                    {
                        PublishedFundingRepository = Policy.NoOpAsync(),
                        SpecificationsApiClient = Policy.NoOpAsync()
                    },
                    Logger.None);
        }

        [TestMethod]
        public async Task ExitsEarlyIfRequestDoesntPassValidation()
        {
            ApplyCustomProfileRequest request = NewApplyCustomProfileRequest();
            
            GivenTheValidationResultForTheRequest(NewValidationResult(_ => 
                    _.WithValidationFailures(NewValidationFailure())), 
                request);
            
            IActionResult result = await WhenTheCustomProfileIsApplied(request, NewAuthor());

            result
                .Should()
                .BeOfType<BadRequestObjectResult>();
            
            AndNoNewVersionWasCreated();
        }

        [TestMethod]
        [DataRow(PublishedProviderStatus.Draft, PublishedProviderStatus.Draft)]
        [DataRow(PublishedProviderStatus.Updated, PublishedProviderStatus.Updated)]
        [DataRow(PublishedProviderStatus.Approved, PublishedProviderStatus.Updated)]
        [DataRow(PublishedProviderStatus.Released, PublishedProviderStatus.Updated)]
        public async Task OverridesProfilePeriodsOnPublishedProviderVersionAndGeneratesNewVersion(PublishedProviderStatus currentStatus,
            PublishedProviderStatus expectedRequestedStatus)
        {
            string fundingLineOne = NewRandomString();
            ProfilePeriod profilePeriod1 = NewProfilePeriod(_ => _.WithDistributionPeriodId("FY-2021"));
            ProfilePeriod profilePeriod2 = NewProfilePeriod(_ => _.WithDistributionPeriodId("FY-2021"));

            ApplyCustomProfileRequest request = NewApplyCustomProfileRequest(_ => _
                .WithFundingLineCode(fundingLineOne)
                .WithProfilePeriods(profilePeriod1, profilePeriod2));

            PublishedProvider publishedProvider = NewPublishedProvider(_ => _.WithCurrent(
                NewPublishedProviderVersion(ppv =>
                ppv.WithPublishedProviderStatus(currentStatus)
                    .WithFundingLines(NewFundingLine(fl =>
                        fl.WithFundingLineCode(fundingLineOne)
                            .WithDistributionPeriods(NewDistributionPeriod(dp =>
                                dp.WithDistributionPeriodId("FY-2021")
                                    .WithProfilePeriods(profilePeriod1, profilePeriod2))))
                        ))));

            Reference author = NewAuthor();

            GivenTheValidationResultForTheRequest(NewValidationResult(), request);
            AndThePublishedProvider(request.PublishedProviderId, publishedProvider);

            IActionResult result = await WhenTheCustomProfileIsApplied(request, author);

            result
                .Should()
                .BeOfType<NoContentResult>();

            IEnumerable<ProfilePeriod> profilePeriods = request.ProfilePeriods;
            FundingLine fundingLine = publishedProvider.Current.FundingLines.Single(fl => fl.FundingLineCode == fundingLineOne);

            AndTheCustomProfilePeriodsWereUsedOn(fundingLine, profilePeriods);
            AndANewProviderVersionWasCreatedFor(publishedProvider, expectedRequestedStatus, author);
            AndProfilingAuditUpdatedForFundingLines(publishedProvider, new[] { fundingLineOne }, author);
        }

        private void AndProfilingAuditUpdatedForFundingLines(PublishedProvider publishedProvider, string[] fundingLines, Reference author)
        {
            foreach (string fundingLineCode in fundingLines)
            {
                publishedProvider
                    .Current
                    .ProfilingAudits
                    .Should()
                    .Contain(a => a.FundingLineCode == fundingLineCode
                                && a.User != null
                                && a.User.Id == author.Id
                                && a.User.Name == author.Name
                                && a.Date.Date == DateTime.Today);
            }
        }

        private void AndTheCustomProfilePeriodsWereUsedOn(FundingLine fundingLine, IEnumerable<ProfilePeriod> profilePeriods)
        {
            fundingLine
                .DistributionPeriods.SelectMany(_ => _.ProfilePeriods)
                .Should()
                .BeEquivalentTo(profilePeriods);
        }

        private void GivenTheValidationResultForTheRequest(ValidationResult result, ApplyCustomProfileRequest request)
        {
            _validator.Setup(_ => _.ValidateAsync(request, default))
                .ReturnsAsync(result);
        }

        private void AndANewProviderVersionWasCreatedFor(PublishedProvider publishedProvider, PublishedProviderStatus newStatus, Reference author)
        {
            _publishedProviderVersionCreation.Verify(_ => _.UpdatePublishedProviderStatus(new [] { publishedProvider },
                author,
                newStatus,
                null,
                null),
                Times.Once);
        }

        private void AndThePublishedProvider(string id, PublishedProvider publishedProvider)
        {
            _publishedFunding.Setup(_ => _.GetPublishedProviderById(id, id))
                .ReturnsAsync(publishedProvider);
        }

        private async Task<IActionResult> WhenTheCustomProfileIsApplied(ApplyCustomProfileRequest request, Reference author)
        {
            return await _service.ApplyCustomProfile(request, author);
        }

        private Reference NewAuthor(Action<ReferenceBuilder> setUp = null)
        {
            ReferenceBuilder referenceBuilder = new ReferenceBuilder();

            setUp?.Invoke(referenceBuilder);
            
            return referenceBuilder.Build();
        }

        private void AndNoNewVersionWasCreated()
        {
            _publishedProviderVersionCreation.Verify(_ => _.UpdatePublishedProviderStatus(It.IsAny<IEnumerable<PublishedProvider>>(),
                It.IsAny<Reference>(),
                It.IsAny<PublishedProviderStatus>(),
                It.IsAny<string>(),
                It.IsAny<string>()),
                Times.Never);
        }

        private ValidationResult NewValidationResult(Action<ValidationResultBuilder> setUp = null)
        {
            ValidationResultBuilder resultBuilder = new ValidationResultBuilder();

            setUp?.Invoke(resultBuilder);
            
            return resultBuilder.Build();
        }

        private ValidationFailure NewValidationFailure(Action<ValidationFailureBuilder> setUp = null)
        {
            ValidationFailureBuilder failureBuilder = new ValidationFailureBuilder();

            setUp?.Invoke(failureBuilder);
            
            return failureBuilder.Build();
        }
    }
}