using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Policies;
using CalculateFunding.Common.ApiClient.Profiling;
using CalculateFunding.Common.ApiClient.Profiling.Models;
using CalculateFunding.Common.Utility;
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
        private readonly AsyncPolicy _profilingResilience;
        private readonly AsyncPolicy _policiesResilience;

        public ProfilePatternPreview(IReProfilingRequestBuilder reProfilingRequestBuilder,
            IProfilingApiClient profiling,
            IPoliciesApiClient policies,
            IPublishingResiliencePolicies resiliencePolicies)
        {
            Guard.ArgumentNotNull(reProfilingRequestBuilder, nameof(reProfilingRequestBuilder));
            Guard.ArgumentNotNull(profiling, nameof(profiling));
            Guard.ArgumentNotNull(resiliencePolicies?.ProfilingApiClient, nameof(resiliencePolicies.ProfilingApiClient));
            Guard.ArgumentNotNull(resiliencePolicies.PoliciesApiClient, nameof(resiliencePolicies.PoliciesApiClient));

            _reProfilingRequestBuilder = reProfilingRequestBuilder;
            _profiling = profiling;
            _policies = policies;
            _profilingResilience = resiliencePolicies.ProfilingApiClient;
            _policiesResilience = resiliencePolicies.PoliciesApiClient;
        }

        public async Task<IActionResult> PreviewProfilingChange(ProfilePreviewRequest request)
        {
            Guard.ArgumentNotNull(request, nameof(request));

            ReProfileRequest reProfileRequest = await _reProfilingRequestBuilder.BuildReProfileRequest(request.FundingStreamId,
                request.SpecificationId,
                request.FundingPeriodId,
                request.ProviderId,
                request.FundingLineCode,
                request.ProfilePatternKey,
                request.ConfigurationType);

            ApiResponse<ReProfileResponse> reProfilingApiResponse = await _profilingResilience.ExecuteAsync(() => _profiling.ReProfile(reProfileRequest));

            ReProfileResponse reProfileResponse = reProfilingApiResponse?.Content;

            if (reProfileResponse == null)
            {
                throw new InvalidOperationException(
                    $"Did not received a valid re-profiling response for profile pattern preview request {request}");
            }

            ExistingProfilePeriod[] existingProfilePeriods = reProfileRequest.ExistingPeriods.ToArray();
            
            int isPaidTo = GetIsPaidTo(existingProfilePeriods);

            ProfileTotal[] profileTotals = BuildProfileTotals(reProfileResponse, existingProfilePeriods, isPaidTo);

            await AddFundingDatesToProfileTotals(request, profileTotals);

            return new OkObjectResult(profileTotals);
        }

        private static ProfileTotal[] BuildProfileTotals(ReProfileResponse reProfileResponse,
            ExistingProfilePeriod[] existingProfilePeriods,
            int isPaidTo)
        {
            return reProfileResponse.DeliveryProfilePeriods.Select((deliveryProfilePeriod,
                    installmentNumber)
                => new ProfileTotal
                {
                    DistributionPeriodId = deliveryProfilePeriod.DistributionPeriod,
                    Occurrence = deliveryProfilePeriod.Occurrence,
                    Value = installmentNumber < isPaidTo ? existingProfilePeriods[installmentNumber].ProfileValue.GetValueOrDefault() : deliveryProfilePeriod.ProfileValue,
                    Year = deliveryProfilePeriod.Year,
                    InstallmentNumber = installmentNumber + 1,
                    IsPaid = installmentNumber < isPaidTo,
                    PeriodType = deliveryProfilePeriod.Type.ToString(),
                    TypeValue = deliveryProfilePeriod.TypeValue
                }).ToArray();
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