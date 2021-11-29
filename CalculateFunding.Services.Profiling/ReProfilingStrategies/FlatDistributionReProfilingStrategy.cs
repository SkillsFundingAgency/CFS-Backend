using System;
using System.Linq;
using CalculateFunding.Services.Profiling.Models;

namespace CalculateFunding.Services.Profiling.ReProfilingStrategies
{
    public abstract class FlatDistributionReProfilingStrategy : ReProfilingStrategy
    {
        // this is the rounding tolerance used to determine whether we overwrite the existing period value
        protected const int ExistingProfileValueTolerance = 1;

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

            DistributeRemainingBalance(variationPointerIndex,
                orderedRefreshProfilePeriods,
                orderedExistingProfilePeriods,
                differenceToDistribute);
        }

        protected void DistributeRemainingBalance(int variationPointerIndex,
            IProfilePeriod[] orderedRefreshProfilePeriods,
            IProfilePeriod[] orderedExistingProfilePeriods,
            decimal differenceToDistribute)
        {
            int remainingPeriodsToPay = orderedRefreshProfilePeriods.Length - variationPointerIndex;

            decimal remainingPeriodsProfileValue = Math.Round(differenceToDistribute / remainingPeriodsToPay, 2, MidpointRounding.AwayFromZero);
            decimal remainderForFinalPeriod = differenceToDistribute - remainingPeriodsToPay * remainingPeriodsProfileValue;

            bool useExisting = false;

            if (differenceToDistribute == 0)
            {
                for (int refreshProfilePeriodIndex = variationPointerIndex; refreshProfilePeriodIndex < orderedRefreshProfilePeriods.Length; refreshProfilePeriodIndex++)
                {
                    IProfilePeriod profilePeriod = orderedRefreshProfilePeriods[refreshProfilePeriodIndex];

                    useExisting = UseExisting(profilePeriod.GetProfileValue() + remainingPeriodsProfileValue,
                        orderedExistingProfilePeriods,
                        refreshProfilePeriodIndex);

                    if (!useExisting)
                    {
                        break;
                    }
                }
            }

            for (int refreshProfilePeriodIndex = variationPointerIndex; refreshProfilePeriodIndex < orderedRefreshProfilePeriods.Length; refreshProfilePeriodIndex++)
            {
                IProfilePeriod profilePeriod = orderedRefreshProfilePeriods[refreshProfilePeriodIndex];

                decimal adjustedProfileValue = useExisting ? 
                    orderedExistingProfilePeriods[refreshProfilePeriodIndex].GetProfileValue() :
                    profilePeriod.GetProfileValue() + remainingPeriodsProfileValue;

                profilePeriod.SetProfiledValue(adjustedProfileValue);
            }

            if (differenceToDistribute == 0) return;

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

        protected static bool UseExisting(decimal refreshProfilePeriodValue,
            IProfilePeriod[] orderedExistingProfilePeriods,
            int profilePeriodIndex)
        {
            decimal existingPeriodValue = orderedExistingProfilePeriods.Length - 1 >= profilePeriodIndex ? 
                    orderedExistingProfilePeriods[profilePeriodIndex].GetProfileValue() :
                    refreshProfilePeriodValue;

            // if the difference between the existing profile value and the new value is a rounding difference then take the existing period value
            return Math.Abs(Math.Abs(refreshProfilePeriodValue) - Math.Abs(existingPeriodValue)) < ExistingProfileValueTolerance ? 
                true : 
                false;
        }
    }
}