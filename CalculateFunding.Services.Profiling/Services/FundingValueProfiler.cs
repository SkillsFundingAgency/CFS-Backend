using System;
using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Services.Profiling.Extensions;
using CalculateFunding.Services.Profiling.Models;

namespace CalculateFunding.Services.Profiling.Services
{
    public class FundingValueProfiler : IFundingValueProfiler
    {
        public AllocationProfileResponse ProfileAllocation(
            ProfileRequestBase request,
            FundingStreamPeriodProfilePattern profilePattern,
            decimal fundingValue)
        {
            IReadOnlyCollection<DeliveryProfilePeriod> profilePeriods = GetProfiledAllocationPeriodsWithPatternApplied(fundingValue,
                profilePattern.ProfilePattern,
                profilePattern.RoundingStrategy);

            IReadOnlyCollection<DistributionPeriods> distributionPeriods = GetDistributionPeriodWithPatternApplied(
                profilePeriods);

            return new AllocationProfileResponse(
                profilePeriods.ToArray(),
                distributionPeriods.ToArray());
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

            return ApplyProfilePattern(fundingValue, profilePattern, allocationProfilePeriods, roundingStrategy);
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
            List<DeliveryProfilePeriod> calculatedDeliveryProfile = new List<DeliveryProfilePeriod>();

            if (profilePeriods.Any())
            {
                decimal allocationValueToBeProfiled = Convert.ToDecimal(fundingValue);

                List<DeliveryProfilePeriod> profiledValues = profilePeriods.Select(pp =>
                {
                    ProfilePeriodPattern profilePeriodPattern = profilePattern.Single(
                        pattern => string.Equals(pattern.Period, pp.TypeValue)
                                   && string.Equals(pattern.DistributionPeriod, pp.DistributionPeriod)
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
                    else
                    {
                        roundedValue = (int) profiledValue;
                    }

                    return pp.WithValue(roundedValue);
                }).ToList();

                DeliveryProfilePeriod last = profiledValues.Last();

                IEnumerable<DeliveryProfilePeriod> withoutLast = profiledValues.Take(profiledValues.Count - 1).ToList();

                calculatedDeliveryProfile.AddRange(
                    withoutLast.Append(
                        last.WithValue(allocationValueToBeProfiled - withoutLast.Sum(cdp => cdp.ProfileValue))));
            }

            return calculatedDeliveryProfile;
        }
    }
}