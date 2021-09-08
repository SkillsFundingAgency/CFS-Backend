using System;
using System.Linq;
using CalculateFunding.Common.Extensions;
using CalculateFunding.Services.Profiling.Models;

namespace CalculateFunding.Services.Profiling.ReProfilingStrategies
{
    public class ReProfileFutureDistributionPeriodsWithAdjustments : ReProfilingStrategy, IReProfilingStrategy
    {
        public string StrategyKey => nameof(ReProfileFutureDistributionPeriodsWithAdjustments);

        public string DisplayName => "ReProfile Future Distribution Periods With Adjustments";

        public string Description => "Adjusts future period allocations based on funding value amount changes";

        public ReProfileStrategyResult ReProfile(ReProfileContext context)
        {
            ReProfileRequest reProfileRequest = context.Request;

            IProfilePeriod[] orderedRefreshProfilePeriods = new YearMonthOrderedProfilePeriods<IProfilePeriod>(context.ProfileResult.DeliveryProfilePeriods)
                .ToArray();
            IExistingProfilePeriod[] orderedExistingProfilePeriods = new YearMonthOrderedProfilePeriods<IExistingProfilePeriod>(reProfileRequest.ExistingPeriods)
                .ToArray();

            DeliveryProfilePeriod[] readonlyOrderedRefreshProfilePeriods = new YearMonthOrderedProfilePeriods<DeliveryProfilePeriod>(context.ProfileResult.DeliveryProfilePeriods)
                .DeepCopy()
                .ToArray();

            int variationPointerIndex = GetVariationPointerIndex(orderedRefreshProfilePeriods, orderedExistingProfilePeriods, context);
            RetainPaidProfilePeriodValues(variationPointerIndex, orderedExistingProfilePeriods, orderedRefreshProfilePeriods);

            AdjustFuturePeriodFundingLineValues(orderedExistingProfilePeriods, variationPointerIndex, reProfileRequest, orderedRefreshProfilePeriods, readonlyOrderedRefreshProfilePeriods);
            
            return new ReProfileStrategyResult
            {
                DistributionPeriods = MapIntoDistributionPeriods(context),
                DeliveryProfilePeriods = context.ProfileResult.DeliveryProfilePeriods,
                CarryOverAmount = 0M
            };
        }

        private static void AdjustFuturePeriodFundingLineValues(IExistingProfilePeriod[] orderedExistingProfilePeriods,
            int variationPointerIndex,
            ReProfileRequest reProfileRequest,
            IProfilePeriod[] orderedRefreshProfilePeriods,
            IProfilePeriod[] readonlyOrderedRefreshProfilePeriods)
        {
            decimal fundingLineTotal = reProfileRequest.FundingLineTotal;
            decimal existingFundingLineTotal = reProfileRequest.ExistingFundingLineTotal;
            
            decimal amountAlreadyPaid = orderedExistingProfilePeriods.Take(variationPointerIndex).Sum(x => x.GetProfileValue());
            decimal amountThatShouldHaveBeenPaid = readonlyOrderedRefreshProfilePeriods.Take(variationPointerIndex)
                                                    .Sum(x => x.GetProfileValue());
            decimal amountToBeAdjustedInNextPeriod = amountThatShouldHaveBeenPaid - amountAlreadyPaid;

            decimal remaingAmount = fundingLineTotal - amountAlreadyPaid;

            if (remaingAmount > 0)
            {
                DistributeRemainingBalance(orderedRefreshProfilePeriods, variationPointerIndex, fundingLineTotal, amountToBeAdjustedInNextPeriod);
            }
            else
            {
                ReclaimPaidAmounts(orderedRefreshProfilePeriods, variationPointerIndex, remaingAmount);
            }
        }

        private static void ReclaimPaidAmounts(IProfilePeriod[] orderedRefreshProfilePeriods, int variationPointerIndex, decimal amountToBeAdjustedInNextPeriod)
        {
            for (int profilePeriod = variationPointerIndex; profilePeriod < orderedRefreshProfilePeriods.Length; profilePeriod++)
            {
                IProfilePeriod periodToAdjust = orderedRefreshProfilePeriods[profilePeriod];

                decimal profileValue = (profilePeriod == variationPointerIndex) ? amountToBeAdjustedInNextPeriod : 0M;

                periodToAdjust.SetProfiledValue(profileValue);
            }
        }

        private static void DistributeRemainingBalance(
            IProfilePeriod[] orderedRefreshProfilePeriods, 
            int variationPointerIndex,
            decimal fundingLineTotal,
            decimal amountToBeAdjustedInNextPeriod)
        {
            IProfilePeriod periodToAdjust = orderedRefreshProfilePeriods[variationPointerIndex];
            
            decimal profileValue = periodToAdjust.GetProfileValue() + amountToBeAdjustedInNextPeriod;

            periodToAdjust.SetProfiledValue(Math.Round(profileValue, 2));

            // Adding any rounding amount to the next profile period
            var adjustRoundingDecimalsAmount = fundingLineTotal - orderedRefreshProfilePeriods.Sum(x => x.GetProfileValue());
            var variationPointerIndexProfile = orderedRefreshProfilePeriods[variationPointerIndex];
            variationPointerIndexProfile.SetProfiledValue(variationPointerIndexProfile.GetProfileValue() + adjustRoundingDecimalsAmount);
        }
    }
}
