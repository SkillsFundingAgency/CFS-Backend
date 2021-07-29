using System;
using System.Linq;
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

            int variationPointerIndex = GetVariationPointerIndex(orderedRefreshProfilePeriods, orderedExistingProfilePeriods, context);
            RetainPaidProfilePeriodValues(variationPointerIndex, orderedExistingProfilePeriods, orderedRefreshProfilePeriods);

            decimal carryOverAmount = AdjustFuturePeriodFundingLineValues(orderedExistingProfilePeriods, variationPointerIndex, reProfileRequest, orderedRefreshProfilePeriods);

            return new ReProfileStrategyResult
            {
                DistributionPeriods = MapIntoDistributionPeriods(context),
                DeliveryProfilePeriods = context.ProfileResult.DeliveryProfilePeriods,
                CarryOverAmount = carryOverAmount
            };
        }

        private static decimal AdjustFuturePeriodFundingLineValues(IExistingProfilePeriod[] orderedExistingProfilePeriods,
            int variationPointerIndex,
            ReProfileRequest reProfileRequest,
            IProfilePeriod[] orderedRefreshProfilePeriods)
        {
            decimal fundingLineTotal = reProfileRequest.FundingLineTotal;
            decimal existingFundingLineTotal = reProfileRequest.ExistingFundingLineTotal;
            
            decimal amountAlreadyPaid = orderedExistingProfilePeriods.Take(variationPointerIndex).Sum(x => x.GetProfileValue());
            decimal amountThatShouldHaveBeenPaid = orderedExistingProfilePeriods.Take(variationPointerIndex)
                                                    .Sum(x => CalculateAmountThatShouldHaveBeenPaid(existingFundingLineTotal, x.GetProfileValue(), fundingLineTotal, orderedExistingProfilePeriods.Length));
            decimal amountToBeAdjustedInNextPeriod = amountThatShouldHaveBeenPaid - amountAlreadyPaid;

            decimal remaingAmount = fundingLineTotal - amountAlreadyPaid;

            if (remaingAmount > 0)
            {
                DistributeRemainingBalance(orderedRefreshProfilePeriods, variationPointerIndex, existingFundingLineTotal, fundingLineTotal, amountToBeAdjustedInNextPeriod);
            }
            else
            {
                ReclaimPaidAmounts(orderedRefreshProfilePeriods, variationPointerIndex, remaingAmount);
            }

            return 0M;
        }

        private static decimal CalculateAmountThatShouldHaveBeenPaid(decimal existingFundingLineTotal, decimal existingProfileValue, decimal fundingLineTotal, int numberofPeriods)
        {
            if (existingFundingLineTotal == 0)
            {
                // if the existing funding total is zero then treat as a flat profile
                return (1M / numberofPeriods) * fundingLineTotal;
            }

            // Calculating same percentages
            return (existingProfileValue / existingFundingLineTotal) * fundingLineTotal;
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
            decimal existingFundingLineTotal,
            decimal fundingLineTotal,
            decimal amountToBeAdjustedInNextPeriod)
        {
            for (int profilePeriod = variationPointerIndex; profilePeriod < orderedRefreshProfilePeriods.Length; profilePeriod++)
            {
                IProfilePeriod periodToAdjust = orderedRefreshProfilePeriods[profilePeriod];
                decimal amountThatShouldHaveBeenPaid = CalculateAmountThatShouldHaveBeenPaid(existingFundingLineTotal, periodToAdjust.GetProfileValue(), fundingLineTotal, orderedRefreshProfilePeriods.Length);

                decimal profileValue = (profilePeriod == variationPointerIndex) ?
                    amountThatShouldHaveBeenPaid + amountToBeAdjustedInNextPeriod :
                    amountThatShouldHaveBeenPaid;

                periodToAdjust.SetProfiledValue(Math.Round(profileValue, 2));
            }

            // Adding any rounding amount to the next profile period
            var adjustRoundingDecimalsAmount = fundingLineTotal - orderedRefreshProfilePeriods.Sum(x => x.GetProfileValue());
            var variationPointerIndexProfile = orderedRefreshProfilePeriods[variationPointerIndex];
            variationPointerIndexProfile.SetProfiledValue(variationPointerIndexProfile.GetProfileValue() + adjustRoundingDecimalsAmount);
        }
    }
}
