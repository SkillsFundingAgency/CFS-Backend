using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Profiling.Models;
using CalculateFunding.Common.ApiClient.Profiling;
using CalculateFunding.Common.ApiClient.Specifications;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Publishing.Interfaces;
using Polly;

namespace CalculateFunding.Services.Publishing.Profiling
{
    public class ReProfilingRequestBuilder : IReProfilingRequestBuilder
    {
        private readonly ISpecificationsApiClient _specifications;
        private readonly IProfilingApiClient _profiling;
        private readonly IPublishedFundingRepository _publishedFunding;
        private readonly AsyncPolicy _specificationResilience;
        private readonly AsyncPolicy _profilingResilience;
        private readonly AsyncPolicy _publishedFundingResilience;

        public ReProfilingRequestBuilder(ISpecificationsApiClient specifications,
            IProfilingApiClient profiling,
            IPublishedFundingRepository publishedFunding,
            IPublishingResiliencePolicies resiliencePolicies)
        {
            Guard.ArgumentNotNull(specifications, nameof(specifications));
            Guard.ArgumentNotNull(profiling, nameof(profiling));
            Guard.ArgumentNotNull(publishedFunding, nameof(publishedFunding));
            Guard.ArgumentNotNull(resiliencePolicies?.SpecificationsApiClient, nameof(resiliencePolicies.SpecificationsApiClient));
            Guard.ArgumentNotNull(resiliencePolicies?.PublishedFundingRepository, nameof(resiliencePolicies.PublishedFundingRepository));
            Guard.ArgumentNotNull(resiliencePolicies?.ProfilingApiClient, nameof(resiliencePolicies.ProfilingApiClient));

            _specifications = specifications;
            _publishedFunding = publishedFunding;
            _profiling = profiling;
            _specificationResilience = resiliencePolicies.SpecificationsApiClient;
            _publishedFundingResilience = resiliencePolicies.PublishedFundingRepository;
            _profilingResilience = resiliencePolicies.ProfilingApiClient;
        }

        public async Task<ReProfileRequest> BuildReProfileRequest(string fundingStreamId,
            string specificationId,
            string fundingPeriodId,
            string providerId,
            string fundingLineCode,
            string profilePatternKey,
            ProfileConfigurationType configurationType,
            decimal? fundingLineTotal = null,
            bool midYear = false)
        {
            Guard.IsNullOrWhiteSpace(specificationId, nameof(specificationId));
            Guard.IsNullOrWhiteSpace(fundingStreamId, nameof(fundingStreamId));
            Guard.IsNullOrWhiteSpace(fundingPeriodId, nameof(fundingPeriodId));
            Guard.IsNullOrWhiteSpace(providerId, nameof(providerId));
            Guard.IsNullOrWhiteSpace(fundingLineCode, nameof(fundingLineCode));

            ProfileVariationPointer profileVariationPointer = await GetProfileVariationPointerForFundingLine(specificationId,
                fundingLineCode,
                fundingStreamId);

            ProfilePeriod[] orderedProfilePeriodsForFundingLine = await GetOrderedProfilePeriodsForFundingLine(fundingStreamId,
                fundingPeriodId,
                providerId,
                fundingLineCode,
                profilePatternKey);

            int paidUpToIndex = GetProfilePeriodIndexForVariationPointer(profileVariationPointer, orderedProfilePeriodsForFundingLine, providerId);

            IEnumerable<ExistingProfilePeriod> existingProfilePeriods = BuildExistingProfilePeriods(orderedProfilePeriodsForFundingLine, paidUpToIndex);

            decimal existingFundingLineTotal = orderedProfilePeriodsForFundingLine.Sum(_ => _.ProfiledValue);
            
            return new ReProfileRequest
            {
                ProfilePatternKey = profilePatternKey,
                ConfigurationType = configurationType,
                FundingLineCode = fundingLineCode,
                FundingPeriodId = fundingPeriodId,
                FundingStreamId = fundingStreamId,
                FundingLineTotal = fundingLineTotal.GetValueOrDefault(existingFundingLineTotal),
                ExistingFundingLineTotal = existingFundingLineTotal,
                ExistingPeriods = existingProfilePeriods,
                MidYear = midYear,
                VariationPointerIndex = paidUpToIndex
            };
        }

        private async Task<ProfilePeriod[]> GetOrderedProfilePeriodsForFundingLine(string fundingStreamId,
            string fundingPeriodId,
            string providerId,
            string fundingLineCode,
            string profilePatternKey)
        {
            PublishedProvider publishedProvider = await _publishedFundingResilience.ExecuteAsync(() => _publishedFunding.GetPublishedProvider(fundingStreamId,
                fundingPeriodId,
                providerId));

            if (publishedProvider != null)
            {
                FundingLine fundingLine = publishedProvider.Current?.FundingLines?.SingleOrDefault(_ => _.FundingLineCode == fundingLineCode);

                if (fundingLine != null && !fundingLine.DistributionPeriods.IsNullOrEmpty())
                {
                    return new YearMonthOrderedProfilePeriods(fundingLine)
                    .ToArray();
                }
            }

            // if this is funded but the existing published provider has a 0 or null funding value
            // then profiling periods will be blank also for a mid-year opener there won't be a published provider
            // so we need to retrieve profile pattern from profiling
            ApiResponse<IEnumerable<FundingStreamPeriodProfilePattern>> apiResponse = await _profilingResilience.ExecuteAsync(() => _profiling.GetProfilePatternsForFundingStreamAndFundingPeriod(fundingStreamId,
                fundingPeriodId));

            if (apiResponse?.Content == null)
            {
                throw new InvalidOperationException(
                                    $"Did not locate any profiling patterns for {fundingStreamId} {fundingPeriodId}");
            }

            IEnumerable<FundingStreamPeriodProfilePattern> profilePatterns = apiResponse.Content;

            FundingStreamPeriodProfilePattern fundingStreamPeriodProfilePattern = profilePatterns?.SingleOrDefault(_ => _.FundingLineId == fundingLineCode && _.ProfilePatternKey == profilePatternKey);

            if (fundingStreamPeriodProfilePattern == null)
            {
                throw new InvalidOperationException(
                                    $"Did not locate a profiling pattern for funding line {fundingLineCode} {fundingStreamId} {fundingPeriodId}");
            }

            return new YearMonthOrderedProfilePeriodPatterns(fundingStreamPeriodProfilePattern.ProfilePattern)
                .Select(_ => new ProfilePeriod { 
                    DistributionPeriodId = _.DistributionPeriod,
                    Occurrence = _.Occurrence,
                    ProfiledValue = 0,
                    Type = _.PeriodType.AsMatchingEnum<ProfilePeriodType>(),
                    TypeValue = _.Period,
                    Year = _.PeriodYear
                    }).ToArray();
        }

        private IEnumerable<ExistingProfilePeriod> BuildExistingProfilePeriods(ProfilePeriod[] profilePeriods, int paidUpToIndex)
        {
            for(int period = 0; period < profilePeriods.Length; period++)
            {
                ProfilePeriod profilePeriod = profilePeriods[period];
                
                yield return new ExistingProfilePeriod
                {
                    DistributionPeriod = profilePeriod.DistributionPeriodId,
                    Occurrence = profilePeriod.Occurrence,
                    Type = profilePeriod.Type.AsMatchingEnum<PeriodType>(),
                    Year = profilePeriod.Year,
                    TypeValue = profilePeriod.TypeValue,
                    ProfileValue = period < paidUpToIndex ? profilePeriod.ProfiledValue : (decimal?)null
                };
            }
        }

        private async Task<ProfileVariationPointer> GetProfileVariationPointerForFundingLine(string specificationId,
            string fundingLineCode,
            string fundingStreamId)
        {
            ApiResponse<IEnumerable<ProfileVariationPointer>> variationPointers = await _specificationResilience.ExecuteAsync(
                () => _specifications.GetProfileVariationPointers(specificationId));

            return variationPointers?.Content?.SingleOrDefault(_ => _.FundingLineId == fundingLineCode &&
                                                                    _.FundingStreamId == fundingStreamId);
        }
            
        protected int GetProfilePeriodIndexForVariationPointer(ProfileVariationPointer variationPointer, ProfilePeriod[] profilePeriods, string providerId)
        {
            if (variationPointer == null)
            {
                return 0;
            }
            
            int variationPointerIndex = profilePeriods.IndexOf(_ => _.Occurrence == variationPointer.Occurrence &&
                                                                    _.Year == variationPointer.Year &&
                                                                    _.TypeValue == variationPointer.TypeValue);

            if (variationPointerIndex == -1)
            {
                throw new ArgumentOutOfRangeException(nameof(variationPointer),
                    $"Did not locate profile period corresponding to variation pointer for funding line id {variationPointer.FundingLineId} against provider: {providerId}");
            }

            return variationPointerIndex;
        }
    }
}