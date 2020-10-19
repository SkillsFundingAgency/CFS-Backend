using System;
using System.Linq;
using CalculateFunding.Services.Profiling.Models;

namespace CalculateFunding.Services.Profiling.ReProfilingStrategies
{
    public class ReProfileDsgFundingLine : IReProfilingStrategy
    {
        public string StrategyKey => "ReProfileDsgFundingLine";

        public string DisplayName => "Re-Profile DSG Funding Lines";

        public string Description => "Adjusts DSG profiles for under or over payment changes when re-profiled";

        public ReProfileStrategyResult ReProfile(ReProfileContext context)
        {
            decimal carryOverAmount = AdjustForUnderOrOverPayment(context);

            return new ReProfileStrategyResult
            {
                DistributionPeriods = context.ProfileResult.DeliveryProfilePeriods.Select(_ => new DistributionPeriods()).ToArray(),
                DeliveryProfilePeriods = context.ProfileResult.DeliveryProfilePeriods,
                CarryOverAmount = carryOverAmount
            };
        }
        
        private decimal AdjustForUnderOrOverPayment(ReProfileContext context)
        {
            decimal carryOverAmount = 0;
            
            ReProfileRequest reProfileRequest = context.Request;
            
            IProfilePeriod[] orderRefreshProfilePeriods = new YearMonthOrderedProfilePeriods<IProfilePeriod>(context.ProfileResult.DeliveryProfilePeriods)
                .ToArray();
            IExistingProfilePeriod[] orderedExistingProfilePeriods = new YearMonthOrderedProfilePeriods<IExistingProfilePeriod>(reProfileRequest.ExistingPeriods)
                .ToArray();

            IExistingProfilePeriod existingProfilePeriod = orderedExistingProfilePeriods.LastOrDefault(_ => _.IsPaid);
            
            int variationPointerIndex = existingProfilePeriod == null ? 0 : Array.IndexOf(orderedExistingProfilePeriods, existingProfilePeriod);

            decimal previousFundingLineValue = reProfileRequest.ExistingFundingLineTotal;
            decimal latestFundingLineValue = reProfileRequest.FundingLineTotal;
            
            decimal fundingChange = latestFundingLineValue - previousFundingLineValue;
            decimal latestPeriodAmount = (int)(latestFundingLineValue / orderRefreshProfilePeriods.Length);

            AdjustPeriodsForFundingAlreadyReleased(variationPointerIndex,
                orderedExistingProfilePeriods,
                orderRefreshProfilePeriods);
            
            if (fundingChange < 0)
            {
                if (AdjustingPeriodsForOverPaymentLeavesRemainder(variationPointerIndex,
                    latestPeriodAmount,
                    orderedExistingProfilePeriods,
                    orderRefreshProfilePeriods,
                    out decimal remainingOverPayment))
                {
                    carryOverAmount = remainingOverPayment;
                }
            }
            else
            {
                AdjustPeriodsForUnderPayment(variationPointerIndex,
                latestPeriodAmount,
                orderedExistingProfilePeriods,
                orderRefreshProfilePeriods);
            }

            return carryOverAmount;
        }

        private void AdjustPeriodsForFundingAlreadyReleased(int variationPointerIndex,
            IExistingProfilePeriod[] orderedExistingProfilePeriods,
            IProfilePeriod[] orderRefreshProfilePeriods)
        {
            for (int profilePeriod = 0; profilePeriod < variationPointerIndex; profilePeriod++)
            {
                orderRefreshProfilePeriods[profilePeriod].SetProfiledValue(orderedExistingProfilePeriods[profilePeriod].GetProfileValue());
            }    
        }

        private void AdjustPeriodsForUnderPayment(int variationPointerIndex,
            decimal latestPeriodAmount,
            IExistingProfilePeriod[] orderedExistingProfilePeriods,
            IProfilePeriod[] orderRefreshProfilePeriods)
        {
            decimal amountAlreadyPaid = orderedExistingProfilePeriods.Take(variationPointerIndex).Sum(_ => _.GetProfileValue());
            decimal amountThatShouldHaveBeenPaid = latestPeriodAmount * variationPointerIndex;

            decimal amountUnderPaid = Math.Abs(amountAlreadyPaid - amountThatShouldHaveBeenPaid);

            IProfilePeriod periodToAdjust = orderRefreshProfilePeriods[variationPointerIndex];

            periodToAdjust.SetProfiledValue(periodToAdjust.GetProfileValue() + amountUnderPaid);
        }

        private bool AdjustingPeriodsForOverPaymentLeavesRemainder(int variationPointerIndex,
            decimal latestPeriodAmount,
            IExistingProfilePeriod[] orderedSnapShotProfilePeriods,
            IProfilePeriod[] orderRefreshProfilePeriods,
            out decimal remainingOverPayment)
        {
            decimal amountAlreadyPaid = orderedSnapShotProfilePeriods.Take(variationPointerIndex).Sum(_ => _.GetProfileValue());
            decimal amountThatShouldHaveBeenPaid = latestPeriodAmount * (variationPointerIndex);

            remainingOverPayment = amountAlreadyPaid - amountThatShouldHaveBeenPaid;

            for (int profilePeriod = variationPointerIndex; profilePeriod < orderRefreshProfilePeriods.Length; profilePeriod++)
            {
                if (remainingOverPayment <= 0)
                {
                    break;
                }

                IProfilePeriod periodToAdjust = orderRefreshProfilePeriods[profilePeriod];

                decimal adjustedProfileValue = periodToAdjust.GetProfileValue() - remainingOverPayment;

                if (adjustedProfileValue < 0)
                {
                    remainingOverPayment = Math.Abs(adjustedProfileValue);
                    adjustedProfileValue = 0;
                }
                else
                {
                    remainingOverPayment = 0;
                }

                periodToAdjust.SetProfiledValue(adjustedProfileValue);
            }
            
            return remainingOverPayment != 0;
        }
    }
}