using System;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Policies;
using CalculateFunding.Common.ApiClient.Profiling;
using CalculateFunding.Common.ApiClient.Profiling.Models;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;
using CalculateFunding.Services.Publishing.Profiling;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Polly;

namespace CalculateFunding.Services.Publishing.UnitTests.Profiling
{
    [TestClass]
    public class ProfilePatternPreviewTests
    {
        private Mock<IReProfilingRequestBuilder> _reProfilingRequestBuilder;
        private Mock<IProfilingApiClient> _profiling;
        private Mock<IPoliciesApiClient> _policies;

        private ProfilePatternPreview _preview;

        [TestInitialize]
        public void SetUp()
        {
            _reProfilingRequestBuilder = new Mock<IReProfilingRequestBuilder>();
            _profiling = new Mock<IProfilingApiClient>();
            _policies = new Mock<IPoliciesApiClient>();

            _preview = new ProfilePatternPreview(_reProfilingRequestBuilder.Object,
                _profiling.Object,
                _policies.Object,
                new ResiliencePolicies
                {
                    PoliciesApiClient = Policy.NoOpAsync(),
                    ProfilingApiClient = Policy.NoOpAsync()
                });
        }

        [TestMethod]
        public void GuardsAgainstARequestNotBeingSupplied()
        {
            Func<Task<IActionResult>> invocation = () => WhenTheProfilePatternChangeIsPreviewed(null);

            invocation
                .Should()
                .Throw<ArgumentNullException>()
                .Which
                .ParamName
                .Should()
                .Be("request");
        }

        [TestMethod]
        public void GuardsAgainstNotGettingAReProfilingResponse()
        {
            ProfilePreviewRequest previewRequest = NewProfilePreviewRequest();

            Func<Task<IActionResult>> invocation = () => WhenTheProfilePatternChangeIsPreviewed(previewRequest);

            invocation
                .Should()
                .Throw<InvalidOperationException>()
                .Which
                .Message
                .Should()
                .Be($"Did not received a valid re-profiling response for profile pattern preview request {previewRequest}");

            AndAReProfileRequestWasBuiltForTheRequest(previewRequest);
        }

        [TestMethod]
        public async Task ReProfilesFundingLineAndMapsResultsIntoProfileTotals()
        {
            ProfilePreviewRequest previewRequest = NewProfilePreviewRequest();

            string distributionPeriod = NewRandomString();

            ReProfileRequest expectedReProfileRequest = NewReProfileRequest(_ => _.WithFundingLineTotal(999)
                .WithExistingFundingLineTotal(1000)
                .WithExistingProfilePeriods(NewExistingProfilePeriod(exp =>
                    exp.WithDistributionPeriod(distributionPeriod)
                        .WithPeriodType(PeriodType.CalendarMonth)
                        .WithTypeValue("January")
                        .WithValue(23)
                        .WithYear(2021)
                        .WithOccurrence(0)),
                    NewExistingProfilePeriod(exp =>
                        exp.WithDistributionPeriod(distributionPeriod)
                            .WithPeriodType(PeriodType.CalendarMonth)
                            .WithTypeValue("January")
                            .WithValue(24)
                            .WithYear(2021)
                            .WithOccurrence(1)),
                    NewExistingProfilePeriod(exp =>
                        exp.WithDistributionPeriod(distributionPeriod)
                            .WithPeriodType(PeriodType.CalendarMonth)
                            .WithTypeValue("February")
                            .WithValue(null)
                            .WithYear(2021)
                            .WithOccurrence(0)),
                    NewExistingProfilePeriod(exp =>
                        exp.WithDistributionPeriod(distributionPeriod)
                            .WithPeriodType(PeriodType.CalendarMonth)
                            .WithTypeValue("March")
                            .WithValue(null)
                            .WithYear(2021)
                            .WithOccurrence(0)),
                    NewExistingProfilePeriod(exp =>
                        exp.WithDistributionPeriod(distributionPeriod)
                            .WithPeriodType(PeriodType.CalendarMonth)
                            .WithTypeValue("April")
                            .WithValue(null)
                            .WithYear(2021)
                            .WithOccurrence(0)),
                    NewExistingProfilePeriod(exp =>
                        exp.WithDistributionPeriod(distributionPeriod)
                            .WithPeriodType(PeriodType.CalendarMonth)
                            .WithTypeValue("May")
                            .WithValue(null)
                            .WithYear(2021)
                            .WithOccurrence(0))
                    ));
            ReProfileResponse expectedReProfileResponse = NewReProfileResponse(_ => _.WithCarryOverAmount(992)
                .WithDeliveryProfilePeriods(NewDeliveryProfilePeriod(exp =>
                    exp.WithDistributionPeriod(distributionPeriod)
                        .WithPeriodType(PeriodType.CalendarMonth)
                        .WithTypeValue("January")
                        .WithValue(33)
                        .WithYear(2021)
                        .WithOccurrence(0)),
                    NewDeliveryProfilePeriod(exp =>
                        exp.WithDistributionPeriod(distributionPeriod)
                            .WithPeriodType(PeriodType.CalendarMonth)
                            .WithTypeValue("January")
                            .WithValue(34)
                            .WithYear(2021)
                            .WithOccurrence(1)),
                    NewDeliveryProfilePeriod(exp =>
                        exp.WithDistributionPeriod(distributionPeriod)
                            .WithPeriodType(PeriodType.CalendarMonth)
                            .WithTypeValue("February")
                            .WithValue(35)
                            .WithYear(2021)
                            .WithOccurrence(0)),
                    NewDeliveryProfilePeriod(exp =>
                        exp.WithDistributionPeriod(distributionPeriod)
                            .WithPeriodType(PeriodType.CalendarMonth)
                            .WithTypeValue("March")
                            .WithValue(36)
                            .WithYear(2021)
                            .WithOccurrence(0)),
                    NewDeliveryProfilePeriod(exp =>
                        exp.WithDistributionPeriod(distributionPeriod)
                            .WithPeriodType(PeriodType.CalendarMonth)
                            .WithTypeValue("April")
                            .WithValue(37)
                            .WithYear(2021)
                            .WithOccurrence(0)),
                    NewDeliveryProfilePeriod(exp =>
                        exp.WithDistributionPeriod(distributionPeriod)
                            .WithPeriodType(PeriodType.CalendarMonth)
                            .WithTypeValue("May")
                            .WithValue(38)
                            .WithYear(2021)
                            .WithOccurrence(0))));

            DateTimeOffset expectedActualDateOne = NewRandomDate();
            DateTimeOffset expectedActualDateTwo = NewRandomDate();
            DateTimeOffset expectedActualDateThree = NewRandomDate();
            DateTimeOffset expectedActualDateFour = NewRandomDate();

            FundingDate expectedFundingDate = NewFundingDate(_ => _.WithPatterns(NewFundingDatePattern(fdp =>
                fdp.WithOccurrence(0)
                    .WithPeriod("January")
                    .WithPeriodYear(2021)
                    .WithPaymentDate(expectedActualDateOne)),
                NewFundingDatePattern(fdp =>
                    fdp.WithOccurrence(0)
                        .WithPeriod("February")
                        .WithPeriodYear(2021)
                        .WithPaymentDate(expectedActualDateTwo)),
            NewFundingDatePattern(fdp =>
                fdp.WithOccurrence(0)
                    .WithPeriod("April")
                    .WithPeriodYear(2021)
                    .WithPaymentDate(expectedActualDateThree)),
            NewFundingDatePattern(fdp =>
                fdp.WithOccurrence(0)
                    .WithPeriod("May")
                    .WithPeriodYear(2021)
                    .WithPaymentDate(expectedActualDateFour)),
                NewFundingDatePattern()));

            GivenTheReProfileRequest(previewRequest, expectedReProfileRequest);
            AndTheReProfileResponse(expectedReProfileRequest, expectedReProfileResponse);
            AndTheFundingDate(previewRequest, expectedFundingDate);

            OkObjectResult response = await WhenTheProfilePatternChangeIsPreviewed(previewRequest) as OkObjectResult;

            response?
                .Value
                .Should()
                .BeEquivalentTo(new[] {
                    NewProfileTotal(_ => _.WithOccurrence(0)
                        .WithDistributionPeriod(distributionPeriod)
                        .WithValue(23)
                        .WithYear(2021)
                        .WithPeriodType("CalendarMonth")
                        .WithTypeValue("January")
                        .WithIsPaid(true)
                        .WithInstallmentNumber(1)
                        .WithActualDate(expectedActualDateOne)),
                    NewProfileTotal(_ => _.WithOccurrence(1)
                        .WithDistributionPeriod(distributionPeriod)
                        .WithValue(24)
                        .WithYear(2021)
                        .WithPeriodType("CalendarMonth")
                        .WithTypeValue("January")
                        .WithIsPaid(true)
                        .WithInstallmentNumber(2)
                        .WithActualDate(null)),
                    NewProfileTotal(_ => _.WithOccurrence(0)
                        .WithDistributionPeriod(distributionPeriod)
                        .WithValue(35)
                        .WithYear(2021)
                        .WithPeriodType("CalendarMonth")
                        .WithTypeValue("February")
                        .WithIsPaid(false)
                        .WithInstallmentNumber(3)
                        .WithActualDate(expectedActualDateTwo)),
                    NewProfileTotal(_ => _.WithOccurrence(0)
                        .WithDistributionPeriod(distributionPeriod)
                        .WithValue(36)
                        .WithYear(2021)
                        .WithPeriodType("CalendarMonth")
                        .WithTypeValue("March")
                        .WithIsPaid(false)
                        .WithInstallmentNumber(4)
                        .WithActualDate(null)),
                    NewProfileTotal(_ => _.WithOccurrence(0)
                        .WithDistributionPeriod(distributionPeriod)
                        .WithValue(37)
                        .WithYear(2021)
                        .WithPeriodType("CalendarMonth")
                        .WithTypeValue("April")
                        .WithIsPaid(false)
                        .WithInstallmentNumber(5)
                        .WithActualDate(expectedActualDateThree)),
                    NewProfileTotal(_ => _.WithOccurrence(0)
                        .WithDistributionPeriod(distributionPeriod)
                        .WithValue(38)
                        .WithYear(2021)
                        .WithPeriodType("CalendarMonth")
                        .WithTypeValue("May")
                        .WithIsPaid(false)
                        .WithInstallmentNumber(6)
                        .WithActualDate(expectedActualDateFour))
                });
        }

        private async Task<IActionResult> WhenTheProfilePatternChangeIsPreviewed(ProfilePreviewRequest request)
            => await _preview.PreviewProfilingChange(request);

        private void AndAReProfileRequestWasBuiltForTheRequest(ProfilePreviewRequest request)
            => _reProfilingRequestBuilder.Verify(_ => _.BuildReProfileRequest(request.FundingStreamId,
                    request.SpecificationId,
                    request.FundingPeriodId,
                    request.ProviderId,
                    request.FundingLineCode,
                    request.ProfilePatternKey,
                    request.ConfigurationType,
                    null,
                    false),
                Times.Once);

        private void GivenTheReProfileRequest(ProfilePreviewRequest previewRequest,
            ReProfileRequest reProfileRequest)
            => _reProfilingRequestBuilder.Setup(_ => _.BuildReProfileRequest(previewRequest.FundingStreamId,
                    previewRequest.SpecificationId,
                    previewRequest.FundingPeriodId,
                    previewRequest.ProviderId,
                    previewRequest.FundingLineCode,
                    previewRequest.ProfilePatternKey,
                    previewRequest.ConfigurationType,
                    null,
                    false))
                .ReturnsAsync(reProfileRequest);

        private void AndTheReProfileResponse(ReProfileRequest request,
            ReProfileResponse expectedResponse)
            => _profiling.Setup(_ => _.ReProfile(request))
                .ReturnsAsync(new ApiResponse<ReProfileResponse>(HttpStatusCode.OK, expectedResponse));

        private void AndTheFundingDate(ProfilePreviewRequest previewRequest,
            FundingDate fundingDate)
            => _policies.Setup(_ => _.GetFundingDate(previewRequest.FundingStreamId,
                    previewRequest.FundingPeriodId,
                    previewRequest.FundingLineCode))
                .ReturnsAsync(new ApiResponse<FundingDate>(HttpStatusCode.OK, fundingDate));

        private ProfilePreviewRequest NewProfilePreviewRequest(Action<ProfilePreviewRequestBuilder> setUp = null)
        {
            ProfilePreviewRequestBuilder profilePreviewRequestBuilder = new ProfilePreviewRequestBuilder();

            setUp?.Invoke(profilePreviewRequestBuilder);

            return profilePreviewRequestBuilder.Build();
        }

        private FundingDatePattern NewFundingDatePattern(Action<FundingDatePatternBuilder> setUp = null)
        {
            FundingDatePatternBuilder fundingDatePatternBuilder = new FundingDatePatternBuilder();

            setUp?.Invoke(fundingDatePatternBuilder);

            return fundingDatePatternBuilder.Build();
        }

        private ReProfileRequest NewReProfileRequest(Action<ReProfileRequestTestEntityBuilder> setUp = null)
        {
            ReProfileRequestTestEntityBuilder requestTestEntityBuilder = new ReProfileRequestTestEntityBuilder();

            setUp?.Invoke(requestTestEntityBuilder);

            return requestTestEntityBuilder.Build();
        }

        private ReProfileResponse NewReProfileResponse(Action<ReProfileResponseBuilder> setUp = null)
        {
            ReProfileResponseBuilder reProfileResponseBuilder = new ReProfileResponseBuilder();

            setUp?.Invoke(reProfileResponseBuilder);

            return reProfileResponseBuilder.Build();
        }

        private FundingDate NewFundingDate(Action<FundingDateBuilder> setUp = null)
        {
            FundingDateBuilder fundingDateBuilder = new FundingDateBuilder();

            setUp?.Invoke(fundingDateBuilder);

            return fundingDateBuilder.Build();
        }

        private ExistingProfilePeriod NewExistingProfilePeriod(Action<ExistingProfilePeriodBuilder> setUp = null)
        {
            ExistingProfilePeriodBuilder existingProfilePeriodBuilder = new ExistingProfilePeriodBuilder();

            setUp?.Invoke(existingProfilePeriodBuilder);

            return existingProfilePeriodBuilder.Build();
        }

        private ProfileTotal NewProfileTotal(Action<ProfileTotalBuilder> setUp = null)
        {
            ProfileTotalBuilder profileTotalBuilder = new ProfileTotalBuilder();

            setUp?.Invoke(profileTotalBuilder);

            return profileTotalBuilder.Build();
        }

        private DeliveryProfilePeriod NewDeliveryProfilePeriod(Action<DeliveryProfilePeriodBuilder> setUp = null)
        {
            DeliveryProfilePeriodBuilder deliveryProfilePeriodBuilder = new DeliveryProfilePeriodBuilder();

            setUp?.Invoke(deliveryProfilePeriodBuilder);

            return deliveryProfilePeriodBuilder.Build();
        }

        private static string NewRandomString() => new RandomString();

        private DateTimeOffset NewRandomDate() => new RandomDateTime();
    }
}