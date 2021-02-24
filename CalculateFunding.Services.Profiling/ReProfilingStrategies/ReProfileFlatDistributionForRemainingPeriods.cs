using System;
using System.Linq;
using CalculateFunding.Services.Profiling.Models;

namespace CalculateFunding.Services.Profiling.ReProfilingStrategies
{
    public class ReProfileFlatDistributionForRemainingPeriods : ReProfilingStrategy, IReProfilingStrategy
    {
        public string StrategyKey => nameof(ReProfileFlatDistributionForRemainingPeriods);
        
        public string DisplayName => "Re-Profile Flat Distribution For Remaining Periods";

        public string Description => "Distributes changes to funding evenly across all of the remaining profile periods";

        public ReProfileStrategyResult ReProfile(ReProfileContext context)
        {
            ReProfileRequest reProfileRequest = context.Request;
            
            IProfilePeriod[] orderedRefreshProfilePeriods = new YearMonthOrderedProfilePeriods<IProfilePeriod>(context.ProfileResult.DeliveryProfilePeriods)
                .ToArray();
            IExistingProfilePeriod[] orderedExistingProfilePeriods = new YearMonthOrderedProfilePeriods<IExistingProfilePeriod>(reProfileRequest.ExistingPeriods)
                .ToArray();

            int variationPointerIndex = GetVariationPointerIndex(orderedRefreshProfilePeriods, orderedExistingProfilePeriods);
            
            RetainPaidProfilePeriodValues(variationPointerIndex, orderedExistingProfilePeriods, orderedRefreshProfilePeriods);

            decimal  carryOverAmount = DistributeRemainingFundingLineValueEvenly(orderedExistingProfilePeriods, variationPointerIndex, reProfileRequest, orderedRefreshProfilePeriods);

            return new ReProfileStrategyResult
            {
                DistributionPeriods = MapIntoDistributionPeriods(context),
                DeliveryProfilePeriods = context.ProfileResult.DeliveryProfilePeriods,
                CarryOverAmount = carryOverAmount
            };
        }

        private static decimal DistributeRemainingFundingLineValueEvenly(IExistingProfilePeriod[] orderedExistingProfilePeriods,
            int variationPointerIndex,
            ReProfileRequest reProfileRequest,
            IProfilePeriod[] orderedRefreshProfilePeriods)
        {
            decimal previousFundingLineValuePaid = orderedExistingProfilePeriods.Take(variationPointerIndex).Sum(_ => _.GetProfileValue());
            decimal remainingFundingLineValueToPay = reProfileRequest.FundingLineTotal - previousFundingLineValuePaid;

            DistributeRemainingBalance(variationPointerIndex, orderedRefreshProfilePeriods, Math.Max(remainingFundingLineValueToPay, 0));

            return remainingFundingLineValueToPay < 0 ? Math.Abs(remainingFundingLineValueToPay) : 0;
        }
        
        private static void DistributeRemainingBalance(int variationPointerIndex,
            IProfilePeriod[] orderedRefreshProfilePeriods,
            decimal remainingFundingLineValueToPay)
        {
            int remainingPeriodsToPay = orderedRefreshProfilePeriods.Length - variationPointerIndex;

            int remainingPeriodsProfileValue = (int) Math.Floor(remainingFundingLineValueToPay / remainingPeriodsToPay);
            decimal remainderForFinalPeriod = remainingFundingLineValueToPay - remainingPeriodsToPay * remainingPeriodsProfileValue;

            for (int refreshProfilePeriodIndex = variationPointerIndex; refreshProfilePeriodIndex < orderedRefreshProfilePeriods.Length; refreshProfilePeriodIndex++)
            {
                IProfilePeriod profilePeriod = orderedRefreshProfilePeriods[refreshProfilePeriodIndex];

                profilePeriod.SetProfiledValue(remainingPeriodsProfileValue);
            }

            IProfilePeriod finalProfilePeriod = orderedRefreshProfilePeriods.Last();

            finalProfilePeriod.SetProfiledValue(finalProfilePeriod.GetProfileValue() + remainderForFinalPeriod);
        }

        private static void RetainPaidProfilePeriodValues(int variationPointerIndex,
            IExistingProfilePeriod[] orderedExistingProfilePeriods,
            IProfilePeriod[] orderedRefreshProfilePeriods)
        {
            for (int paidProfilePeriodIndex = 0; paidProfilePeriodIndex < variationPointerIndex; paidProfilePeriodIndex++)
            {
                IProfilePeriod paidProfilePeriod = orderedExistingProfilePeriods[paidProfilePeriodIndex];
                IProfilePeriod refreshProfilePeriod = orderedRefreshProfilePeriods[paidProfilePeriodIndex];

                refreshProfilePeriod.SetProfiledValue(paidProfilePeriod.GetProfileValue());
            }
        }
    }
}