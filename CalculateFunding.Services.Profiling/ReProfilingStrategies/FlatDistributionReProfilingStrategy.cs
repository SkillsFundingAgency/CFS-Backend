using System;
using System.Linq;
using CalculateFunding.Services.Profiling.Models;

namespace CalculateFunding.Services.Profiling.ReProfilingStrategies
{
    public abstract class FlatDistributionReProfilingStrategy : ReProfilingStrategy
    {
        protected static decimal CalculateCarryOverAmount(ReProfileRequest reProfileRequest,
            IProfilePeriod[] orderedRefreshProfilePeriods)
        {
            return reProfileRequest.FundingLineTotal - orderedRefreshProfilePeriods.Sum(_ => _.GetProfileValue());
        }

        protected void DistributeRemainingFundingLineValueEvenly(IExistingProfilePeriod[] orderedExistingProfilePeriods,
            int variationPointerIndex,
            ReProfileRequest reProfileRequest,
            IProfilePeriod[] orderedRefreshProfilePeriods)
        {
            decimal previousFundingLineValuePaid = orderedExistingProfilePeriods.Take(variationPointerIndex).Sum(_ => _.GetProfileValue());
            decimal remainingFundingLineValueToPay = reProfileRequest.FundingLineTotal - previousFundingLineValuePaid;
            decimal remainingFundingLineProfiledToPay = orderedRefreshProfilePeriods.Skip(variationPointerIndex).Sum(_ => _.GetProfileValue());
            decimal differenceToDistribute = remainingFundingLineValueToPay - remainingFundingLineProfiledToPay;

            DistributeRemainingBalance(variationPointerIndex, orderedRefreshProfilePeriods, differenceToDistribute);
        }

        protected void DistributeRemainingBalance(int variationPointerIndex,
            IProfilePeriod[] orderedRefreshProfilePeriods,
            decimal differenceToDistribute)
        {
            int remainingPeriodsToPay = orderedRefreshProfilePeriods.Length - variationPointerIndex;

            decimal remainingPeriodsProfileValue = Math.Round(differenceToDistribute / remainingPeriodsToPay, 2, MidpointRounding.AwayFromZero);
            decimal remainderForFinalPeriod = differenceToDistribute - remainingPeriodsToPay * remainingPeriodsProfileValue;

            for (int refreshProfilePeriodIndex = variationPointerIndex; refreshProfilePeriodIndex < orderedRefreshProfilePeriods.Length; refreshProfilePeriodIndex++)
            {
                IProfilePeriod profilePeriod = orderedRefreshProfilePeriods[refreshProfilePeriodIndex];

                decimal adjustedProfileValue = profilePeriod.GetProfileValue() + remainingPeriodsProfileValue;

                profilePeriod.SetProfiledValue(adjustedProfileValue);
            }

            IProfilePeriod finalProfilePeriod = orderedRefreshProfilePeriods.Last();

            finalProfilePeriod.SetProfiledValue(finalProfilePeriod.GetProfileValue() + remainderForFinalPeriod);
        }

        protected ReProfileStrategyResult FlatDistribution(ReProfileContext context,
            IProfilePeriod[] orderedRefreshProfilePeriods,
            IExistingProfilePeriod[] orderedExistingProfilePeriods,
            ReProfileRequest reProfileRequest,
            int variationPointerIndex)
        {
            RetainPaidProfilePeriodValues(variationPointerIndex, orderedExistingProfilePeriods, orderedRefreshProfilePeriods);
            DistributeRemainingFundingLineValueEvenly(orderedExistingProfilePeriods,
                variationPointerIndex,
                reProfileRequest,
                orderedRefreshProfilePeriods);

            decimal carryOverAmount = CalculateCarryOverAmount(reProfileRequest, orderedRefreshProfilePeriods);

            return new ReProfileStrategyResult
            {
                DistributionPeriods = MapIntoDistributionPeriods(context),
                DeliveryProfilePeriods = context.ProfileResult.DeliveryProfilePeriods,
                CarryOverAmount = carryOverAmount
            };
        }
    }
}