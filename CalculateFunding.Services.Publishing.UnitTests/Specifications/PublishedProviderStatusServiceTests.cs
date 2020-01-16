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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using CalculateFunding.Services.Publishing.Models;

namespace CalculateFunding.Services.Publishing.UnitTests.Specifications
{
    [TestClass]
    public class PublishedProviderStatusServiceTests
    {
        private PublishedProviderStatusService _service;

        private ISpecificationIdServiceRequestValidator _validator;
        private ValidationResult _validationResult;
        private ISpecificationService _specificationService;
        private IPublishedFundingRepository _publishedFundingRepository;

        private string _specificationId;
        private string _providerType;
        private string _localAuthority;
        private string _status;

        private IActionResult _actionResult;

        private SpecificationSummary _specificationSummary;

        [TestInitialize]
        public void SetUp()
        {
            _specificationId = NewRandomString();
            _validationResult = new ValidationResult();

            _validator = Substitute.For<ISpecificationIdServiceRequestValidator>();
            _validator.Validate(_specificationId)
                .Returns(_validationResult);

            _specificationService = Substitute.For<ISpecificationService>();
            _publishedFundingRepository = Substitute.For<IPublishedFundingRepository>();

            _service = new PublishedProviderStatusService(_validator, _specificationService, _publishedFundingRepository, new ResiliencePolicies
            {
                PublishedFundingRepository = Polly.Policy.NoOpAsync(),
                SpecificationsRepositoryPolicy = Polly.Policy.NoOpAsync()
            });
        }

        [TestMethod]
        public async Task ReturnsBadRequestWhenSuppliedSpecificationIdFailsValidation()
        {
            string[] expectedErrors = { NewRandomString(), NewRandomString() };

            GivenTheValidationErrors(expectedErrors);

            await WhenThePublishedProvidersStatusAreQueried();

            ThenTheResponseShouldBe<BadRequestObjectResult>();
        }

        [TestMethod]
        public async Task ReturnsThePublishedProviderStatuFromPublishedProviderRepository()
        {
            string fundingStreamId1 = NewRandomString();
            string fundingStreamId2 = NewRandomString();

            const string approvedStatus = "Approved";
            const string draftStatus = "Draft";
            const string releasedStatus = "Released";
            const string updatedStatus = "Updated";

            int fs1ApprovedCount = NewRandomNumber();
            int fs1DraftCount = NewRandomNumber();
            int fs1ReleasedCount = NewRandomNumber();
            int fs1UpdatedCount = NewRandomNumber();

            int fs2ApprovedCount = NewRandomNumber();
            int fs2DraftCount = NewRandomNumber();
            int fs2ReleasedCount = NewRandomNumber();
            int fs2UpdatedCount = NewRandomNumber();

            decimal fs1ApprovedTotalFunding = NewRandomNumber();
            decimal fs1DraftTotalFunding = NewRandomNumber();
            decimal fs1ReleasedTotalFunding = NewRandomNumber();
            decimal fs1UpdatedTotalFunding = NewRandomNumber();

            decimal fs2ApprovedTotalFunding = NewRandomNumber();
            decimal fs2DraftTotalFunding = NewRandomNumber();
            decimal fs2ReleasedTotalFunding = NewRandomNumber();
            decimal fs2UpdatedTotalFunding = NewRandomNumber();

            ProviderFundingStreamStatusResponse firstExpectedResponse = NewProviderFundingStreamStatusResponse(_ => _
                .WithFundingStreamId(fundingStreamId1)
                .WithProviderApprovedCount(fs1ApprovedCount)
                .WithProviderReleasedCount(fs1ReleasedCount)
                .WithProviderUpdatedCount(fs1UpdatedCount)
                .WithProviderDraftCount(fs1DraftCount)
                .WithTotalFunding(fs1ApprovedTotalFunding + fs1DraftTotalFunding + fs1ReleasedTotalFunding + fs1UpdatedTotalFunding));

            ProviderFundingStreamStatusResponse secondExpectedResponse = NewProviderFundingStreamStatusResponse(_ => _
                .WithFundingStreamId(fundingStreamId2)
                .WithProviderApprovedCount(fs2ApprovedCount)
                .WithProviderReleasedCount(fs2ReleasedCount)
                .WithProviderUpdatedCount(fs2UpdatedCount)
                .WithProviderDraftCount(fs2DraftCount)
                .WithTotalFunding(fs2ApprovedTotalFunding + fs2DraftTotalFunding + fs2ReleasedTotalFunding + fs2UpdatedTotalFunding));

            GivenThePublishedProvidersForTheSpecificationId(
                NewPublishedProviderFundingStreamStatus(_ => _.WithFundingStreamId(fundingStreamId1).WithCount(fs1ApprovedCount).WithStatus(approvedStatus).WithTotalFunding(fs1ApprovedTotalFunding)),
                NewPublishedProviderFundingStreamStatus(_ => _.WithFundingStreamId(fundingStreamId1).WithCount(fs1DraftCount).WithStatus(draftStatus).WithTotalFunding(fs1DraftTotalFunding)),
                NewPublishedProviderFundingStreamStatus(_ => _.WithFundingStreamId(fundingStreamId1).WithCount(fs1ReleasedCount).WithStatus(releasedStatus).WithTotalFunding(fs1ReleasedTotalFunding)),
                NewPublishedProviderFundingStreamStatus(_ => _.WithFundingStreamId(fundingStreamId1).WithCount(fs1UpdatedCount).WithStatus(updatedStatus).WithTotalFunding(fs1UpdatedTotalFunding)),
                NewPublishedProviderFundingStreamStatus(_ => _.WithFundingStreamId(fundingStreamId2).WithCount(fs2ApprovedCount).WithStatus(approvedStatus).WithTotalFunding(fs2ApprovedTotalFunding)),
                NewPublishedProviderFundingStreamStatus(_ => _.WithFundingStreamId(fundingStreamId2).WithCount(fs2DraftCount).WithStatus(draftStatus).WithTotalFunding(fs2DraftTotalFunding)),
                NewPublishedProviderFundingStreamStatus(_ => _.WithFundingStreamId(fundingStreamId2).WithCount(fs2ReleasedCount).WithStatus(releasedStatus).WithTotalFunding(fs2ReleasedTotalFunding)),
                NewPublishedProviderFundingStreamStatus(_ => _.WithFundingStreamId(fundingStreamId2).WithCount(fs2UpdatedCount).WithStatus(updatedStatus).WithTotalFunding(fs2UpdatedTotalFunding)));

            AndTheSpecificationSummaryIsRetrieved(NewSpecificationSummary(s =>
            {
                s.WithId(_specificationId);
                s.WithFundingStreamIds(new[] { fundingStreamId1, fundingStreamId2 });
            }));

            await WhenThePublishedProvidersStatusAreQueried();

            ThenTheResponseShouldBe<OkObjectResult>(_ =>
                ((IEnumerable<ProviderFundingStreamStatusResponse>)_.Value).SequenceEqual(new[]
                {
                    firstExpectedResponse,
                    secondExpectedResponse
                }, new ProviderFundingStreamStatusResponseComparer()));
        }

        private void AndTheSpecificationSummaryIsRetrieved(SpecificationSummary specificationSummary)
        {
            _specificationSummary = specificationSummary;
            _specificationService
                .GetSpecificationSummaryById(Arg.Is(_specificationId))
                .Returns(_specificationSummary);
        }

        private PublishedProviderFundingStreamStatus NewPublishedProviderFundingStreamStatus(Action<PublishedProviderFundingStreamStatusBuilder> setUp = null)
        {
            PublishedProviderFundingStreamStatusBuilder builder = new PublishedProviderFundingStreamStatusBuilder();

            setUp?.Invoke(builder);

            return builder.Build();
        }

        private ProviderFundingStreamStatusResponse NewProviderFundingStreamStatusResponse(Action<ProviderFundingStreamStatusResponseBuilder> setUp = null)
        {
            ProviderFundingStreamStatusResponseBuilder builder = new ProviderFundingStreamStatusResponseBuilder();

            setUp?.Invoke(builder);

            return builder.Build();
        }

        private void GivenThePublishedProvidersForTheSpecificationId(params PublishedProviderFundingStreamStatus[] publishedProviderFundingStreamStatuses )
        {
            _publishedFundingRepository.GetPublishedProviderStatusCounts(_specificationId, _providerType, _localAuthority, _status)
                .Returns(publishedProviderFundingStreamStatuses);
        }

        private async Task WhenThePublishedProvidersStatusAreQueried()
        {
            _actionResult = await _service.GetProviderStatusCounts(_specificationId, _providerType, _localAuthority, _status);
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

        private string NewRandomString()
        {
            return new RandomString();
        }

        private int NewRandomNumber()
        {
            return new RandomNumberBetween(1, 10000);
        }

        private SpecificationSummary NewSpecificationSummary(Action<SpecificationSummaryBuilder> setUp = null)
        {
            SpecificationSummaryBuilder builder = new SpecificationSummaryBuilder();

            setUp?.Invoke(builder);

            return builder.Build();
        }

    }
}
