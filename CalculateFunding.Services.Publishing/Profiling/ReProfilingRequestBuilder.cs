using System;
using System.Collections.Generic;
using System.Globalization;
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
using CalculateFunding.Services.Publishing.Variations.Strategies;
using Polly;

namespace CalculateFunding.Services.Publishing.Profiling
{
    public class ReProfilingRequestBuilder : IReProfilingRequestBuilder
    {
        private readonly ISpecificationsApiClient _specifications;
        private readonly AsyncPolicy _specificationResilience;

        public ReProfilingRequestBuilder(ISpecificationsApiClient specifications,
            IPublishingResiliencePolicies resiliencePolicies)
        {
            Guard.ArgumentNotNull(specifications, nameof(specifications));
            Guard.ArgumentNotNull(resiliencePolicies?.SpecificationsApiClient, nameof(resiliencePolicies.SpecificationsApiClient));
            
            _specifications = specifications;
            _specificationResilience = resiliencePolicies.SpecificationsApiClient;
        }

        public async Task<ReProfileRequest> BuildReProfileRequest(string fundingLineCode,
            string profilePatternKey,
            PublishedProviderVersion publishedProviderVersion,
            ProfileConfigurationType configurationType,
            decimal? fundingLineTotal = null,
            MidYearType? midYearType = null)
        {
            Guard.ArgumentNotNull(publishedProviderVersion, nameof(publishedProviderVersion));
            Guard.IsNullOrWhiteSpace(fundingLineCode, nameof(fundingLineCode));

            ProfileVariationPointer profileVariationPointer = await GetProfileVariationPointerForFundingLine(publishedProviderVersion.SpecificationId,
                fundingLineCode,
                publishedProviderVersion.FundingStreamId);

            ProfilePeriod[] orderedProfilePeriodsForFundingLine = GetOrderedProfilePeriodsForFundingLine(fundingLineCode,
                publishedProviderVersion);

            if (orderedProfilePeriodsForFundingLine.IsNullOrEmpty())
            {
                throw new ArgumentOutOfRangeException(nameof(publishedProviderVersion), $"Did not locate profile periods corresponding to funding line id {fundingLineCode} against published provider: {publishedProviderVersion.ProviderId}");
            }

            int paidUpToIndex = GetProfilePeriodIndexForVariationPointer(profileVariationPointer, orderedProfilePeriodsForFundingLine, publishedProviderVersion.ProviderId);

            IEnumerable<ExistingProfilePeriod> existingProfilePeriods = BuildExistingProfilePeriods(orderedProfilePeriodsForFundingLine, paidUpToIndex);

            decimal existingFundingLineTotal = orderedProfilePeriodsForFundingLine.Sum(_ => _.ProfiledValue);
            
            return new ReProfileRequest
            {
                ProfilePatternKey = profilePatternKey,
                ConfigurationType = configurationType,
                FundingLineCode = fundingLineCode,
                FundingPeriodId = publishedProviderVersion.FundingPeriodId,
                FundingStreamId = publishedProviderVersion.FundingStreamId,
                FundingLineTotal = fundingLineTotal.GetValueOrDefault(existingFundingLineTotal),
                ExistingFundingLineTotal = existingFundingLineTotal,
                ExistingPeriods = existingProfilePeriods,
                AllExistingPeriods = BuildAllExistingProfilePeriods(orderedProfilePeriodsForFundingLine),
                MidYearType = midYearType,
                VariationPointerIndex = paidUpToIndex
            };
        }

        private ProfilePeriod[] GetOrderedProfilePeriodsForFundingLine(string fundingLineCode,
            PublishedProviderVersion publishedProviderVersion)
        {
            FundingLine fundingLine = publishedProviderVersion.FundingLines?.SingleOrDefault(_ => _.FundingLineCode == fundingLineCode);

            Guard.ArgumentNotNull(fundingLine, nameof(fundingLine));

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

        private IEnumerable<ExistingProfilePeriod> BuildAllExistingProfilePeriods(ProfilePeriod[] profilePeriods)
        {
            for (int period = 0; period < profilePeriods.Length; period++)
            {
                ProfilePeriod profilePeriod = profilePeriods[period];

                yield return new ExistingProfilePeriod
                {
                    DistributionPeriod = profilePeriod.DistributionPeriodId,
                    Occurrence = profilePeriod.Occurrence,
                    Type = profilePeriod.Type.AsMatchingEnum<PeriodType>(),
                    Year = profilePeriod.Year,
                    TypeValue = profilePeriod.TypeValue,
                    ProfileValue = profilePeriod.ProfiledValue
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
                // if the variation pointer cannot be located then pick the last period which is less than the variation pointer date
                ProfilePeriod profilePeriod = profilePeriods.LastOrDefault(_ =>
                {
                    DateTime dateTime = new DateTime(_.Year, DateTime.ParseExact(_.TypeValue, "MMMM", CultureInfo.CurrentCulture).Month, _.Occurrence + 1);
                    DateTime vpDateTime = new DateTime(variationPointer.Year, DateTime.ParseExact(variationPointer.TypeValue, "MMMM", CultureInfo.CurrentCulture).Month, variationPointer.Occurrence + 1);
                    return dateTime < vpDateTime;
                });

                if (profilePeriod == null)
                {
                    throw new ArgumentOutOfRangeException(nameof(variationPointer),
                    $"Did not locate profile period corresponding to variation pointer for funding line id {variationPointer.FundingLineId} against provider: {providerId}");
                }

                return profilePeriods.IndexOf(_ => _.Occurrence == profilePeriod.Occurrence &&
                                                                    _.Year == profilePeriod.Year &&
                                                                    _.TypeValue == profilePeriod.TypeValue) + 1;
            }

            return variationPointerIndex;
        }
    }
}