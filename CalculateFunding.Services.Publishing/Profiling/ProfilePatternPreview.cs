using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Policies;
using CalculateFunding.Common.ApiClient.Profiling;
using CalculateFunding.Common.ApiClient.Profiling.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;
using Microsoft.AspNetCore.Mvc;
using Polly;

namespace CalculateFunding.Services.Publishing.Profiling
{
    public class ProfilePatternPreview : IProfilePatternPreview
    {
        private readonly IReProfilingRequestBuilder _reProfilingRequestBuilder;
        private readonly IProfilingApiClient _profiling;
        private readonly IPoliciesApiClient _policies;
        private readonly IPoliciesService _policiesService;
        private readonly IProfileTotalsService _profileTotalsService;
        private readonly IPublishedFundingRepository _publishedFunding;
        private readonly AsyncPolicy _profilingResilience;
        private readonly AsyncPolicy _policiesResilience;
        private readonly AsyncPolicy _publishedFundingResilience;

        public ProfilePatternPreview(IReProfilingRequestBuilder reProfilingRequestBuilder,
            IProfilingApiClient profiling,
            IPoliciesApiClient policies,
            IPublishedFundingRepository publishedFunding,
            IPublishingResiliencePolicies resiliencePolicies,
            IPoliciesService policiesService,
            IProfileTotalsService profileTotalsService)
        {
            Guard.ArgumentNotNull(reProfilingRequestBuilder, nameof(reProfilingRequestBuilder));
            Guard.ArgumentNotNull(profiling, nameof(profiling));
            Guard.ArgumentNotNull(publishedFunding, nameof(publishedFunding));
            Guard.ArgumentNotNull(resiliencePolicies?.ProfilingApiClient, nameof(resiliencePolicies.ProfilingApiClient));
            Guard.ArgumentNotNull(resiliencePolicies?.PoliciesApiClient, nameof(resiliencePolicies.PoliciesApiClient));
            Guard.ArgumentNotNull(resiliencePolicies?.PublishedFundingRepository, nameof(resiliencePolicies.PublishedFundingRepository));
            Guard.ArgumentNotNull(policiesService, nameof(policiesService));
            Guard.ArgumentNotNull(profileTotalsService, nameof(profileTotalsService));

            _reProfilingRequestBuilder = reProfilingRequestBuilder;
            _profiling = profiling;
            _policies = policies;
            _profilingResilience = resiliencePolicies.ProfilingApiClient;
            _policiesResilience = resiliencePolicies.PoliciesApiClient;
            _publishedFundingResilience = resiliencePolicies.PublishedFundingRepository;
            _policiesService = policiesService;
            _profileTotalsService = profileTotalsService;
            _publishedFunding = publishedFunding;
        }

        public async Task<IActionResult> PreviewProfilingChange(ProfilePreviewRequest request)
        {
            Guard.ArgumentNotNull(request, nameof(request));

            PublishedProviderVersion publishedProviderVersion = (await _publishedFundingResilience.ExecuteAsync(() => _publishedFunding.GetPublishedProvider(request.FundingStreamId,
                request.FundingPeriodId,
                request.ProviderId)))?.Current;

            if (publishedProviderVersion == null)
            {
                throw new InvalidOperationException(
                    $"There is no released version for Provider: {request.ProviderId}, FundingStream: {request.FundingStreamId} and FundingPeriod: {request.FundingPeriodId}.");
            }

            ReProfileRequest reProfileRequest = await _reProfilingRequestBuilder.BuildReProfileRequest(request.FundingLineCode,
                request.ProfilePatternKey,
                publishedProviderVersion,
                request.ConfigurationType);

            ApiResponse<ReProfileResponse> reProfilingApiResponse = await _profilingResilience.ExecuteAsync(() => _profiling.ReProfile(reProfileRequest));

            ReProfileResponse reProfileResponse = reProfilingApiResponse?.Content;

            if (reProfileResponse == null)
            {
                throw new InvalidOperationException(
                    $"Did not received a valid re-profiling response for profile pattern preview request {request}");
            }

            FundingDate fundingDates = await _policiesService.GetFundingDate(
               request.FundingStreamId,
               request.FundingPeriodId,
               request.FundingLineCode);

            FundingLineProfile fundingLineProfile = (await _profileTotalsService.GetPublishedProviderProfileTotalsForSpecificationForProviderForFundingLine(
                request.SpecificationId,
                request.ProviderId,
                request.FundingStreamId,
                request.FundingLineCode)).Value;

            ExistingProfilePeriod[] existingProfilePeriods = reProfileRequest.ExistingPeriods.ToArray();

            int isPaidTo = GetIsPaidTo(existingProfilePeriods);

            ProfileTotal[] profileTotals = BuildProfileTotals(reProfileResponse, existingProfilePeriods, isPaidTo, fundingDates, fundingLineProfile);

            await AddFundingDatesToProfileTotals(request, profileTotals);

            return new OkObjectResult(profileTotals);
        }

        private static ProfileTotal[] BuildProfileTotals(ReProfileResponse reProfileResponse,
            ExistingProfilePeriod[] existingProfilePeriods,
            int isPaidTo,
            FundingDate fundingDates,
            FundingLineProfile fundingLineProfile)
        {
            IEnumerable<ProfileTotal> profileTotals = reProfileResponse.DeliveryProfilePeriods.Select((deliveryProfilePeriod,
                    installmentNumber)
                =>
            {
                decimal value = installmentNumber < isPaidTo ? existingProfilePeriods[installmentNumber].ProfileValue.GetValueOrDefault() : deliveryProfilePeriod.ProfileValue;
                bool isPaid = installmentNumber < isPaidTo;
                return new ProfileTotal
                {
                    DistributionPeriodId = deliveryProfilePeriod.DistributionPeriod,
                    Occurrence = deliveryProfilePeriod.Occurrence,
                    Value = value,
                    Year = deliveryProfilePeriod.Year,
                    InstallmentNumber = installmentNumber + 1,
                    IsPaid = isPaid,
                    PeriodType = deliveryProfilePeriod.Type.ToString(),
                    ActualDate = fundingDates?.Patterns?.SingleOrDefault(_ =>
                        _.Occurrence == deliveryProfilePeriod.Occurrence &&
                        _.Period == deliveryProfilePeriod.TypeValue &&
                        _.PeriodYear == deliveryProfilePeriod.Year)?.PaymentDate,
                    TypeValue = deliveryProfilePeriod.TypeValue,
                    ProfileRemainingPercentage = isPaid ? null :
                        (fundingLineProfile.ProfilePatternTotal.HasValue && fundingLineProfile.ProfilePatternTotal > 0 ?
                            value / (fundingLineProfile.ProfilePatternTotal - fundingLineProfile.AmountAlreadyPaid) * 100 : 0)
                };
            });

            return profileTotals.ToArray();
        }

        private int GetIsPaidTo(ExistingProfilePeriod[] existingProfilePeriods)
        {
            for (int period = 0; period < existingProfilePeriods.Length; period++)
            {
                if (!existingProfilePeriods[period].ProfileValue.HasValue)
                {
                    return period;
                }
            }

            return 0;
        }

        private async Task AddFundingDatesToProfileTotals(ProfilePreviewRequest request,
            IEnumerable<ProfileTotal> profileTotals)
        {
            ApiResponse<FundingDate> fundingDateApiResponse = await _policiesResilience.ExecuteAsync(() => _policies.GetFundingDate(request.FundingStreamId,
                request.FundingPeriodId,
                request.FundingLineCode));

            IEnumerable<FundingDatePattern> fundingDatePatterns = fundingDateApiResponse?.Content?.Patterns;

            foreach (ProfileTotal profileTotal in profileTotals ?? ArraySegment<ProfileTotal>.Empty)
            {
                profileTotal.ActualDate = fundingDatePatterns?.SingleOrDefault(_ =>
                    _.Occurrence == profileTotal.Occurrence &&
                    _.Period == profileTotal.TypeValue &&
                    _.PeriodYear == profileTotal.Year)?.PaymentDate;
            }
        }
    }
}