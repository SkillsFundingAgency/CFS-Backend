using System;
using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Services.Profiling.Extensions;
using CalculateFunding.Services.Profiling.Models;
using CalculateFunding.Services.Profiling.ReProfilingStrategies;

namespace CalculateFunding.Services.Profiling.Services
{
    public class FundingValueProfiler : IFundingValueProfiler
    {
        public AllocationProfileResponse ProfileAllocation(
            ProfileRequestBase request,
            FundingStreamPeriodProfilePattern profilePattern,
            decimal fundingValue)
        {
            if (profilePattern == null)
            {
                throw new InvalidOperationException($"Profile pattern is null, {request}");
            }

            IReadOnlyCollection<DeliveryProfilePeriod> profilePeriods = GetProfiledAllocationPeriodsWithPatternApplied(fundingValue,
                profilePattern.ProfilePattern,
                profilePattern.RoundingStrategy);

            IReadOnlyCollection<DistributionPeriods> distributionPeriods = GetDistributionPeriodWithPatternApplied(
                profilePeriods);

            return new AllocationProfileResponse(
                profilePeriods.ToArray(),
                distributionPeriods.ToArray())
            {
                ProfilePatternKey = profilePattern.ProfilePatternKey,
                ProfilePatternDisplayName = profilePattern.ProfilePatternDisplayName
            };
        }

        private IReadOnlyCollection<DistributionPeriods> GetDistributionPeriodWithPatternApplied(
            IReadOnlyCollection<DeliveryProfilePeriod> profilePattern)
        {
            IReadOnlyCollection<DeliveryProfilePeriod> allocationProfilePeriods =
                GetDistributionPeriodForAllocation(profilePattern);

            return ApplyDistributionPeriodsProfilePattern(allocationProfilePeriods);
        }

        private IReadOnlyCollection<DeliveryProfilePeriod> GetDistributionPeriodForAllocation(
            IReadOnlyCollection<DeliveryProfilePeriod> profilePattern)
        {
            return profilePattern
                .Select(ppp => DeliveryProfilePeriod.CreateInstance(ppp.TypeValue, ppp.Occurrence, ppp.Type, ppp.Year, ppp.ProfileValue, ppp.DistributionPeriod))
                .ToList();
        }

        private IReadOnlyCollection<DistributionPeriods> ApplyDistributionPeriodsProfilePattern(
            IReadOnlyCollection<DeliveryProfilePeriod> profilePeriods)
        {
            List<DistributionPeriods> calculatedDeliveryProfile = new List<DistributionPeriods>();


            if (profilePeriods.Any())
            {
                IReadOnlyCollection<TotalByDistributionPeriod> totalByDistributionPeriod =
                    GetTotalDistributionPeriods(profilePeriods);

                foreach (TotalByDistributionPeriod requestPeriod in totalByDistributionPeriod)
                {
                    calculatedDeliveryProfile.Add(new DistributionPeriods
                    {
                        Value = requestPeriod.Value,
                        DistributionPeriodCode = requestPeriod.DistributionPeriodCode
                    });
                }
            }

            return calculatedDeliveryProfile;
        }

        private IReadOnlyCollection<TotalByDistributionPeriod> GetTotalDistributionPeriods(
            IReadOnlyCollection<DeliveryProfilePeriod> profilePeriods)
        {
            return profilePeriods
                .Select(p => p.DistributionPeriod)
                .Distinct()
                .Select(distributionPeriod => GetTotalForDistributionPeriod(profilePeriods, distributionPeriod))
                .ToList();
        }

        private TotalByDistributionPeriod GetTotalForDistributionPeriod(
            IReadOnlyCollection<DeliveryProfilePeriod> profilePeriods,
            string distributionPeriod)
        {
            IReadOnlyCollection<DeliveryProfilePeriod> matchedPatterns =
                GetMatchingProfilePatterns(profilePeriods, distributionPeriod);

            return new TotalByDistributionPeriod(distributionPeriod, matchedPatterns.Sum(mp => mp.ProfileValue));
        }

        private IReadOnlyCollection<DeliveryProfilePeriod> GetMatchingProfilePatterns(IReadOnlyCollection<DeliveryProfilePeriod> periods,
            string distributionPeriod)
        {
            return periods.Where(period =>
                    period.DistributionPeriod == distributionPeriod)
                .ToList();
        }

        private IReadOnlyCollection<DeliveryProfilePeriod> GetProfiledAllocationPeriodsWithPatternApplied(decimal fundingValue,
            IReadOnlyCollection<ProfilePeriodPattern> profilePattern,
            RoundingStrategy roundingStrategy)
        {
            IReadOnlyCollection<DeliveryProfilePeriod> allocationProfilePeriods =
                GetProfilePeriodsForAllocation(profilePattern);

            return fundingValue == 0 ? 
                    allocationProfilePeriods : 
                    ApplyProfilePattern(fundingValue, profilePattern, allocationProfilePeriods, roundingStrategy);
        }

        private IReadOnlyCollection<DeliveryProfilePeriod> GetProfilePeriodsForAllocation(
            IReadOnlyCollection<ProfilePeriodPattern> profilePattern)
        {
            return profilePattern
                .Select(ppp => DeliveryProfilePeriod.CreateInstance(ppp.Period, ppp.Occurrence, ppp.PeriodType, ppp.PeriodYear, 0m, ppp.DistributionPeriod))
                .Distinct()
                .ToList();
        }

        private IReadOnlyCollection<DeliveryProfilePeriod> ApplyProfilePattern(
            decimal fundingValue,
            IReadOnlyCollection<ProfilePeriodPattern> profilePattern,
            IReadOnlyCollection<DeliveryProfilePeriod> profilePeriods,
            RoundingStrategy roundingStrategy)
        {
            bool negativeAllocation = false;

            List<DeliveryProfilePeriod> calculatedDeliveryProfile = new List<DeliveryProfilePeriod>();

            if (profilePeriods.Any())
            {
                if (fundingValue < 0)
                {
                    fundingValue = Math.Abs(fundingValue);
                    negativeAllocation = true;
                }

                decimal allocationValueToBeProfiled = Convert.ToDecimal(fundingValue);
                decimal runningTotal = 0;

                List<DeliveryProfilePeriod> profiledValues = profilePeriods.Select(pp =>
                {
                    ProfilePeriodPattern profilePeriodPattern = profilePattern.Single(
                        pattern => string.Equals(pattern.Period, pp.TypeValue)
                                   && string.Equals(pattern.DistributionPeriod, pp.DistributionPeriod)
                                   && pattern.PeriodYear == pp.Year
                                   && pattern.Occurrence == pp.Occurrence);

                    decimal profilePercentage = profilePeriodPattern
                        .PeriodPatternPercentage;

                    decimal profiledValue = profilePercentage * allocationValueToBeProfiled;
                    if (profiledValue != 0)
                    {
                        profiledValue /= 100;
                    }

                    decimal roundedValue;


                    if (roundingStrategy == RoundingStrategy.RoundUp)
                    {
                        roundedValue = profiledValue
                            .RoundToDecimalPlaces(2)
                            .RoundToDecimalPlaces(0);
                    }
                    else if (roundingStrategy == RoundingStrategy.MidpointTwoDecimalPlaces)
                    {
                        roundedValue = profiledValue
                            .RoundToDecimalPlaces(2);
                    }
                    else
                    {
                        roundedValue = (int)profiledValue;
                    }

                    if (runningTotal + roundedValue > allocationValueToBeProfiled)
                    {
                        roundedValue = allocationValueToBeProfiled - runningTotal;
                    }

                    runningTotal += roundedValue;

                    return pp.WithValue(roundedValue);
                }).ToList();

                IEnumerable<DeliveryProfilePeriod> orderedDeliveryProfilePeriods = new YearMonthOrderedProfilePeriods<DeliveryProfilePeriod>(profiledValues);
                
                DeliveryProfilePeriod lastUsedProfilePeriod = orderedDeliveryProfilePeriods.LastOrDefault(p => p.ProfileValue > 0) ?? orderedDeliveryProfilePeriods.Last();

                IEnumerable<DeliveryProfilePeriod> withoutLast = profiledValues
                        .Where(p => !(p.Year == lastUsedProfilePeriod.Year && p.TypeValue == lastUsedProfilePeriod.TypeValue && p.Occurrence == lastUsedProfilePeriod.Occurrence));

                calculatedDeliveryProfile.AddRange(
                    new YearMonthOrderedProfilePeriods<DeliveryProfilePeriod>(
                        withoutLast.Append(
                            lastUsedProfilePeriod.WithValue(allocationValueToBeProfiled - withoutLast.Sum(cdp => cdp.ProfileValue)))));
            }

            if (negativeAllocation)
            {
                calculatedDeliveryProfile.ForEach(_ => _.SetProfiledValue(_.ProfileValue * -1));
            }

            return calculatedDeliveryProfile;
        }
    }
}