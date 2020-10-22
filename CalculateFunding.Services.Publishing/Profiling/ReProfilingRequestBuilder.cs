using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Profiling.Models;
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
        private readonly IPublishedFundingRepository _publishedFunding;
        private readonly AsyncPolicy _specificationResilience;
        private readonly AsyncPolicy _publishedFundingResilience;

        public ReProfilingRequestBuilder(ISpecificationsApiClient specifications,
            IPublishedFundingRepository publishedFunding,
            IPublishingResiliencePolicies resiliencePolicies)
        {
            Guard.ArgumentNotNull(specifications, nameof(specifications));
            Guard.ArgumentNotNull(publishedFunding, nameof(publishedFunding));
            Guard.ArgumentNotNull(resiliencePolicies?.SpecificationsApiClient, nameof(resiliencePolicies.SpecificationsApiClient));
            Guard.ArgumentNotNull(resiliencePolicies.PublishedFundingRepository, nameof(resiliencePolicies.PublishedFundingRepository));
            
            _specifications = specifications;
            _publishedFunding = publishedFunding;
            _specificationResilience = resiliencePolicies.SpecificationsApiClient;
            _publishedFundingResilience = resiliencePolicies.PublishedFundingRepository;
        }

        public async Task<ReProfileRequest> BuildReProfileRequest(string specificationId,
            string fundingStreamId,
            string fundingPeriodId,
            string providerId,
            string fundingLineCode,
            string profilePatternKey,
            ProfileConfigurationType configurationType,
            decimal? fundingLineTotal = null)
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
                fundingLineCode);

            int paidUpToIndex = GetProfilePeriodIndexForVariationPointer(profileVariationPointer, orderedProfilePeriodsForFundingLine);

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
                ExistingPeriods = existingProfilePeriods
            };
        }

        private async Task<ProfilePeriod[]> GetOrderedProfilePeriodsForFundingLine(string fundingStreamId,
            string fundingPeriodId,
            string providerId,
            string fundingLineCode)
        {
            PublishedProvider publishedProvider = await _publishedFundingResilience.ExecuteAsync(() => _publishedFunding.GetPublishedProvider(fundingStreamId,
                fundingPeriodId,
                providerId));

            if (publishedProvider == null)
            {
                throw new InvalidOperationException($"Did not locate a published provider for {fundingStreamId} {fundingPeriodId} {providerId}");
            }

            FundingLine fundingLine = publishedProvider.Current?.FundingLines?.SingleOrDefault(_ => _.FundingLineCode == fundingLineCode);
            
            if (fundingLine == null)
            {
                throw new InvalidOperationException(
                    $"Did not locate a funding line {fundingLineCode} on published provider for {fundingStreamId} {fundingPeriodId} {providerId}");
            }

            return new YearMonthOrderedProfilePeriods(fundingLine)
                .ToArray();
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
            
        protected int GetProfilePeriodIndexForVariationPointer(ProfileVariationPointer variationPointer, ProfilePeriod[] profilePeriods)
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
                    $"Did not locate profile period corresponding to variation pointer for funding line id {variationPointer.FundingLineId}");
            }

            return variationPointerIndex;
        }
    }
}