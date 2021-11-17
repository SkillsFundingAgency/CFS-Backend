using System;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Policies;
using CalculateFunding.Common.ApiClient.Profiling;
using CalculateFunding.Common.ApiClient.Profiling.Models;
using CalculateFunding.Models.Publishing;
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
        private Mock<IPoliciesService> _policiesService;
        private Mock<IProfileTotalsService> _profileTotalsService;
        private Mock<IPublishedFundingRepository> _publishedFundingRepository;

        private ProfilePatternPreview _preview;

        [TestInitialize]
        public void SetUp()
        {
            _reProfilingRequestBuilder = new Mock<IReProfilingRequestBuilder>();
            _profiling = new Mock<IProfilingApiClient>();
            _policies = new Mock<IPoliciesApiClient>();
            _policiesService = new Mock<IPoliciesService>();
            _profileTotalsService = new Mock<IProfileTotalsService>();
            _publishedFundingRepository = new Mock<IPublishedFundingRepository>();

            _preview = new ProfilePatternPreview(_reProfilingRequestBuilder.Object,
                _profiling.Object,
                _policies.Object,
                _publishedFundingRepository.Object,
                new ResiliencePolicies
                {
                    PoliciesApiClient = Policy.NoOpAsync(),
                    ProfilingApiClient = Policy.NoOpAsync(),
                    PublishedFundingRepository = Policy.NoOpAsync()
                },
                _policiesService.Object,
                _profileTotalsService.Object);
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

            PublishedProvider publishedProvider = NewPublishedProvider(_ => _.WithCurrent(NewPublisherProviderVersion()));
            
            GivenThePublishedProvider(previewRequest.FundingStreamId,
                previewRequest.FundingPeriodId,
                previewRequest.ProviderId,
                publishedProvider);

            invocation
                .Should()
                .Throw<InvalidOperationException>()
                .Which
                .Message
                .Should()
                .Be($"Did not received a valid re-profiling response for profile pattern preview request {previewRequest}");

            AndAReProfileRequestWasBuiltForTheRequest(previewRequest, publishedProvider.Current);
        }

        [TestMethod]
        public async Task ReProfilesFundingLineAndMapsResultsIntoProfileTotals()
        {
            ProfilePreviewRequest previewRequest = NewProfilePreviewRequest();

            string distributionPeriod = NewRandomString();
            string fundingLineCode = NewRandomString();

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

            FundingLineProfile expectedFundingLineProfile = NewFundingLineProfile(_ =>
                _.WithProfilePatternTotal(1000)
                .WithAmountAlreadyPaid(100)
            );

            PublishedProvider publishedProvider = NewPublishedProvider(_ => _.WithCurrent(NewPublisherProviderVersion(pvp =>
                    pvp.WithFundingLines(NewFundingLine(),
                        NewFundingLine(fl => fl.WithFundingLineCode(fundingLineCode)
                            .WithDistributionPeriods(NewDistributionPeriod(dp =>
                                dp.WithProfilePeriods(NewProfilePeriod(pp => pp.WithDistributionPeriodId("dp1")
                                    .WithAmount(23)
                                    .WithOccurence(0)
                                    .WithYear(2021)
                                    .WithType(ProfilePeriodType.CalendarMonth)
                                    .WithTypeValue("January")),
                                    NewProfilePeriod(pp => pp.WithDistributionPeriodId("dp1")
                                        .WithAmount(24)
                                        .WithOccurence(1)
                                        .WithYear(2021)
                                        .WithType(ProfilePeriodType.CalendarMonth)
                                        .WithTypeValue("January")),
                                    NewProfilePeriod(pp => pp.WithDistributionPeriodId("dp1")
                                        .WithAmount(25)
                                        .WithOccurence(0)
                                        .WithYear(2021)
                                        .WithType(ProfilePeriodType.CalendarMonth)
                                        .WithTypeValue("March")),
                                    NewProfilePeriod(pp => pp.WithDistributionPeriodId("dp1")
                                        .WithAmount(26)
                                        .WithOccurence(0)
                                        .WithYear(2021)
                                        .WithType(ProfilePeriodType.CalendarMonth)
                                        .WithTypeValue("April"))
                                    ))))))));

            GivenThePublishedProvider(previewRequest.FundingStreamId,
                previewRequest.FundingPeriodId,
                previewRequest.ProviderId,
                publishedProvider);

            GivenTheReProfileRequest(previewRequest, expectedReProfileRequest, publishedProvider.Current);
            AndTheReProfileResponse(expectedReProfileRequest, expectedReProfileResponse);
            AndTheProfileTotalsResponse(previewRequest, expectedFundingLineProfile);
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
                        .WithProfileRemainingPercentage(3.8888888888888888888888888900M)
                        .WithActualDate(expectedActualDateTwo)),
                    NewProfileTotal(_ => _.WithOccurrence(0)
                        .WithDistributionPeriod(distributionPeriod)
                        .WithValue(36)
                        .WithYear(2021)
                        .WithPeriodType("CalendarMonth")
                        .WithTypeValue("March")
                        .WithIsPaid(false)
                        .WithInstallmentNumber(4)
                        .WithProfileRemainingPercentage(4.00M)
                        .WithActualDate(null)),
                    NewProfileTotal(_ => _.WithOccurrence(0)
                        .WithDistributionPeriod(distributionPeriod)
                        .WithValue(37)
                        .WithYear(2021)
                        .WithPeriodType("CalendarMonth")
                        .WithTypeValue("April")
                        .WithIsPaid(false)
                        .WithInstallmentNumber(5)
                        .WithProfileRemainingPercentage(4.1111111111111111111111111100M)
                        .WithActualDate(expectedActualDateThree)),
                    NewProfileTotal(_ => _.WithOccurrence(0)
                        .WithDistributionPeriod(distributionPeriod)
                        .WithValue(38)
                        .WithYear(2021)
                        .WithPeriodType("CalendarMonth")
                        .WithTypeValue("May")
                        .WithIsPaid(false)
                        .WithInstallmentNumber(6)
                        .WithProfileRemainingPercentage(4.2222222222222222222222222200M)
                        .WithActualDate(expectedActualDateFour))
                });
        }

        private void GivenThePublishedProvider(string fundingStreamId,
            string fundingPeriodId,
            string providerId,
            PublishedProvider publishedProvider)
            => _publishedFundingRepository.Setup(_ => _.GetPublishedProvider(fundingStreamId,
                    fundingPeriodId,
                    providerId))
                .ReturnsAsync(publishedProvider);

        private async Task<IActionResult> WhenTheProfilePatternChangeIsPreviewed(ProfilePreviewRequest request)
            => await _preview.PreviewProfilingChange(request);

        private void AndAReProfileRequestWasBuiltForTheRequest(ProfilePreviewRequest request, PublishedProviderVersion publishedProviderVersion)
            => _reProfilingRequestBuilder.Verify(_ => _.BuildReProfileRequest(request.FundingLineCode,
                    request.ProfilePatternKey,
                    publishedProviderVersion,
                    request.ConfigurationType,
                    null,
                    null),
                Times.Once);

        private void GivenTheReProfileRequest(ProfilePreviewRequest previewRequest,
            ReProfileRequest reProfileRequest,
            PublishedProviderVersion publishedProviderVersion)
            => _reProfilingRequestBuilder.Setup(_ => _.BuildReProfileRequest(previewRequest.FundingLineCode,
                    previewRequest.ProfilePatternKey,
                    publishedProviderVersion,
                    previewRequest.ConfigurationType,
                    null,
                    null))
                .ReturnsAsync(reProfileRequest);

        private void AndTheReProfileResponse(ReProfileRequest request,
            ReProfileResponse expectedResponse)
            => _profiling.Setup(_ => _.ReProfile(request))
                .ReturnsAsync(new ApiResponse<ReProfileResponse>(HttpStatusCode.OK, expectedResponse));

        private void AndTheProfileTotalsResponse(ProfilePreviewRequest request, FundingLineProfile response)
            => _profileTotalsService.Setup(_ => _.GetPublishedProviderProfileTotalsForSpecificationForProviderForFundingLine(
                request.SpecificationId, request.ProviderId, request.FundingStreamId, request.FundingLineCode))
                .ReturnsAsync(new ActionResult<FundingLineProfile>(response));

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

        private FundingLineProfile NewFundingLineProfile(Action<FundingLineProfileBuilder> setUp = null)
        {
            FundingLineProfileBuilder fundingDateBuilder = new FundingLineProfileBuilder();

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
        private PublishedProvider NewPublishedProvider(Action<PublishedProviderBuilder> setUp = null)
        {
            PublishedProviderBuilder publishedProviderBuilder = new PublishedProviderBuilder();

            setUp?.Invoke(publishedProviderBuilder);

            return publishedProviderBuilder.Build();
        }

        private PublishedProviderVersion NewPublisherProviderVersion(Action<PublishedProviderVersionBuilder> setUp = null)
        {
            PublishedProviderVersionBuilder publishedProviderVersionBuilder = new PublishedProviderVersionBuilder();

            setUp?.Invoke(publishedProviderVersionBuilder);

            return publishedProviderVersionBuilder.Build();
        }

        private FundingLine NewFundingLine(Action<FundingLineBuilder> setUp = null)
        {
            FundingLineBuilder fundingLineBuilder = new FundingLineBuilder();

            setUp?.Invoke(fundingLineBuilder);

            return fundingLineBuilder.Build();
        }

        private DistributionPeriod NewDistributionPeriod(Action<DistributionPeriodBuilder> setUp = null)
        {
            DistributionPeriodBuilder distributionPeriodBuilder = new DistributionPeriodBuilder();

            setUp?.Invoke(distributionPeriodBuilder);

            return distributionPeriodBuilder.Build();
        }

        private ProfilePeriod NewProfilePeriod(Action<ProfilePeriodBuilder> setUp = null)
        {
            ProfilePeriodBuilder profilePeriodBuilder = new ProfilePeriodBuilder();

            setUp?.Invoke(profilePeriodBuilder);

            return profilePeriodBuilder.Build();
        }

        private static string NewRandomString() => new RandomString();

        private DateTimeOffset NewRandomDate() => new RandomDateTime();
    }
}