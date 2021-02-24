using System;
using System.Linq;
using CalculateFunding.Services.Profiling.Models;

namespace CalculateFunding.Services.Profiling.ReProfilingStrategies
{
    public class ReProfileDsgFundingLine : ReProfilingStrategy, IReProfilingStrategy
    {
        public string StrategyKey => nameof(ReProfileDsgFundingLine);

        public string DisplayName => "Re-Profile DSG Funding Lines";

        public string Description => "Adjusts DSG profiles for under or over payment changes when re-profiled";

        public ReProfileStrategyResult ReProfile(ReProfileContext context)
        {
            decimal carryOverAmount = AdjustForUnderOrOverPayment(context);

            //TODO; put this change under test for the distribution periods stuff
            return new ReProfileStrategyResult
            {
                DistributionPeriods = MapIntoDistributionPeriods(context),
                DeliveryProfilePeriods = context.ProfileResult.DeliveryProfilePeriods,
                CarryOverAmount = carryOverAmount
            };
        }

        private decimal AdjustForUnderOrOverPayment(ReProfileContext context)
        {
            decimal carryOverAmount = 0;
            
            ReProfileRequest reProfileRequest = context.Request;
            
            IProfilePeriod[] orderedRefreshProfilePeriods = new YearMonthOrderedProfilePeriods<IProfilePeriod>(context.ProfileResult.DeliveryProfilePeriods)
                .ToArray();
            IExistingProfilePeriod[] orderedExistingProfilePeriods = new YearMonthOrderedProfilePeriods<IExistingProfilePeriod>(reProfileRequest.ExistingPeriods)
                .ToArray();

            int variationPointerIndex = GetVariationPointerIndex(orderedRefreshProfilePeriods, orderedExistingProfilePeriods);

            decimal previousFundingLineValuePaid = orderedExistingProfilePeriods.Take(variationPointerIndex).Sum(_ => _.GetProfileValue());
            decimal latestFundingLineValuePaid = orderedRefreshProfilePeriods.Take(variationPointerIndex).Sum(_ => _.GetProfileValue());
            decimal latestFundingLineValue = reProfileRequest.FundingLineTotal;
            
            decimal fundingChange = latestFundingLineValuePaid - previousFundingLineValuePaid;
            decimal latestPeriodAmount = (int)(latestFundingLineValue / orderedRefreshProfilePeriods.Length);

            AdjustPeriodsForFundingAlreadyReleased(variationPointerIndex,
                orderedExistingProfilePeriods,
                orderedRefreshProfilePeriods);
            
            if (fundingChange < 0)
            {
                if (AdjustingPeriodsForOverPaymentLeavesRemainder(variationPointerIndex,
                    latestPeriodAmount,
                    orderedExistingProfilePeriods,
                    orderedRefreshProfilePeriods,
                    out decimal remainingOverPayment))
                {
                    carryOverAmount = remainingOverPayment;
                }
            }
            else if (fundingChange > 0)
            {
                AdjustPeriodsForUnderPayment(variationPointerIndex,
                latestPeriodAmount,
                orderedExistingProfilePeriods,
                orderedRefreshProfilePeriods);
            }
            else
            {
                AdjustPeriodsForNoTotalAllocationChange(variationPointerIndex,
                    latestPeriodAmount,
                    orderedExistingProfilePeriods,
                    orderedRefreshProfilePeriods,
                    out carryOverAmount);     
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
        
        private void AdjustPeriodsForNoTotalAllocationChange(int variationPointerIndex,
            decimal latestPeriodAmount,
            IExistingProfilePeriod[] orderedExistingProfilePeriods,
            IProfilePeriod[] orderedRefreshProfilePeriods,
            out decimal carryOver)
        {
            decimal amountAlreadyPaid = orderedExistingProfilePeriods.Take(variationPointerIndex).Sum(_ => _.GetProfileValue());
            decimal amountThatShouldHaveBeenPaid = latestPeriodAmount * variationPointerIndex;

            decimal runningTotalChange = amountThatShouldHaveBeenPaid - amountAlreadyPaid;

            if (runningTotalChange < 0)
            {
                AdjustingPeriodsForOverPaymentLeavesRemainder(variationPointerIndex,
                    latestPeriodAmount,
                    orderedExistingProfilePeriods,
                    orderedRefreshProfilePeriods, 
                    out decimal overPaymentCarryOver);

                carryOver = overPaymentCarryOver;
                
                return;
            }
            else if (runningTotalChange > 0)
            {
                AdjustPeriodsForUnderPayment(variationPointerIndex,
                    latestPeriodAmount,
                    orderedExistingProfilePeriods,
                    orderedRefreshProfilePeriods);
            }

            carryOver = 0;
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