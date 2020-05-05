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
using FluentAssertions.Collections;
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
        public async Task ExistsEarlyIfRequestDoesntPassValidation()
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
            string fundingLineTwo = NewRandomString();
            
            ApplyCustomProfileRequest request = NewApplyCustomProfileRequest(_ => _.WithProfileOverrides(
                NewFundingLineProfileOverrides(flo => flo.WithFundingLineCode(fundingLineOne)
                    .WithDistributionPeriods(NewDistributionPeriod(dp => 
                        dp.WithProfilePeriods(NewProfilePeriod(), NewProfilePeriod())))),
                NewFundingLineProfileOverrides(flo => flo.WithFundingLineCode(fundingLineTwo)
                    .WithDistributionPeriods(NewDistributionPeriod(dp => 
                        dp.WithProfilePeriods(NewProfilePeriod(), NewProfilePeriod()))))
                ));
            
            PublishedProvider publishedProvider = NewPublishedProvider(_ => _.WithCurrent(
                NewPublishedProviderVersion(ppv => 
                ppv.WithPublishedProviderStatus(currentStatus)
                    .WithFundingLines(NewFundingLine(fl =>
                        fl.WithFundingLineCode(fundingLineOne)
                            .WithDistributionPeriods(NewDistributionPeriod(dp => 
                                dp.WithProfilePeriods(NewProfilePeriod(), NewProfilePeriod())),
                                NewDistributionPeriod(dp => 
                                    dp.WithProfilePeriods(NewProfilePeriod(), NewProfilePeriod())))),
                        NewFundingLine(fl =>
                            fl.WithFundingLineCode(fundingLineTwo)
                                .WithDistributionPeriods(NewDistributionPeriod(dp => 
                                        dp.WithProfilePeriods(NewProfilePeriod(), NewProfilePeriod())),
                                    NewDistributionPeriod(dp => 
                                        dp.WithProfilePeriods(NewProfilePeriod(), NewProfilePeriod()))))
                        ))));

            Reference author = NewAuthor();
            
            GivenTheValidationResultForTheRequest(NewValidationResult(), request);
            AndThePublishedProvider(request.PublishedProviderId, publishedProvider);

            IActionResult result = await WhenTheCustomProfileIsApplied(request, author);

            result
                .Should()
                .BeOfType<NoContentResult>();

            Dictionary<string, IEnumerable<ProfilePeriod>> customProfiles = request.ProfileOverrides.ToDictionary(_ => _.FundingLineCode,
                _ => _.DistributionPeriods.SelectMany(dp => dp.ProfilePeriods));
            Dictionary<string, FundingLine> fundingLines = publishedProvider.Current.FundingLines.ToDictionary(_ => _.FundingLineCode);

            AndTheCustomProfilePeriodsWereUsedOn(fundingLineOne, fundingLines, customProfiles);
            AndTheCustomProfilePeriodsWereUsedOn(fundingLineTwo, fundingLines, customProfiles);
            AndANewProviderVersionWasCreatedFor(publishedProvider, expectedRequestedStatus, author);            
        }

        private void AndTheCustomProfilePeriodsWereUsedOn(string fundingLineCode, 
            IDictionary<string, FundingLine> fundingLines, 
            IDictionary<string, IEnumerable<ProfilePeriod>> customProfiles)
        {
            fundingLines[fundingLineCode]
                .DistributionPeriods.SelectMany(_ => _.ProfilePeriods)
                .Should()
                .BeEquivalentTo(customProfiles[fundingLineCode]);
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