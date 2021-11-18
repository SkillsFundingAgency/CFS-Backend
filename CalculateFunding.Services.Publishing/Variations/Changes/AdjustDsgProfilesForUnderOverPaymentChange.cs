using System;
using System.IO;
using System.Linq;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Publishing.Models;
using CalculateFunding.Services.Publishing.Profiling;

namespace CalculateFunding.Services.Publishing.Variations.Changes
{
    public class AdjustDsgProfilesForUnderOverPaymentChange : ProfileVariationPointerChange
    {
        public AdjustDsgProfilesForUnderOverPaymentChange(ProviderVariationContext variationContext, string strategyName) 
            : base(variationContext, "adjust profiles for dsg total allocation", strategyName)
        {
        }

        protected override void MakeAdjustmentsFromProfileVariationPointer(ProfileVariationPointer variationPointer)
        {
            PublishedProvider previousSnapshot = VariationContext.GetPublishedProviderOriginalSnapShot(ProviderId);

            if (previousSnapshot == null)
            {
                return;
            }

            string fundingLineId = variationPointer.FundingLineId;
            
            FundingLine latestFundingLine = RefreshState.FundingLines?
                .SingleOrDefault(_ => _.FundingLineCode == fundingLineId);
            FundingLine previousFundingLine = previousSnapshot.Current?.FundingLines?
                .SingleOrDefault(_ => _.FundingLineCode == fundingLineId);
            
            if (latestFundingLine == null || previousFundingLine == null)
            {
                RecordErrors($"Did not locate all funding lines for variation pointer with fundingLineId {fundingLineId}");
               
                return;
            }

            if (latestFundingLine.Value == null && previousFundingLine.Value == null)
            {
                return;
            }
            
            ProfilePeriod[] orderedRefreshProfilePeriods = new YearMonthOrderedProfilePeriods(latestFundingLine)
                .ToArray();
            ProfilePeriod[] orderedSnapShotProfilePeriods = new YearMonthOrderedProfilePeriods(previousFundingLine)
                .ToArray();
            
            int variationPointerIndex = GetProfilePeriodIndexForVariationPoint(variationPointer, orderedRefreshProfilePeriods);

            decimal previousFundingLineValuePaid = orderedSnapShotProfilePeriods.Take(variationPointerIndex).Sum(_ => _.ProfiledValue);
            decimal latestFundingLineValuePaid = orderedRefreshProfilePeriods.Take(variationPointerIndex).Sum(_ => _.ProfiledValue);
            decimal latestFundingLineValue = latestFundingLine.Value.GetValueOrDefault();

            decimal fundingChange = latestFundingLineValuePaid - previousFundingLineValuePaid;
            decimal latestPeriodAmount = (int)(latestFundingLineValue / orderedRefreshProfilePeriods.Length);

            AdjustPeriodsForFundingAlreadyReleased(variationPointerIndex,
                orderedSnapShotProfilePeriods,
                orderedRefreshProfilePeriods);
            
            if (fundingChange < 0)
            {
                if (AdjustingPeriodsForOverPaymentLeavesRemainder(variationPointerIndex,
                    latestPeriodAmount,
                    orderedSnapShotProfilePeriods,
                    orderedRefreshProfilePeriods,
                    out decimal remainingOverPayment))
                {
                    if (remainingOverPayment > 0)
                    {
                        RefreshState.AddCarryOver(fundingLineId, ProfilingCarryOverType.DSGReProfiling, remainingOverPayment);
                    }
                }
            }
            else if (fundingChange > 0)
            {
                AdjustPeriodsForUnderPayment(variationPointerIndex,
                latestPeriodAmount,
                orderedSnapShotProfilePeriods,
                orderedRefreshProfilePeriods);
            }
            else
            {
                AdjustPeriodsForNoTotalAllocationChange(variationPointerIndex,
                    latestPeriodAmount,
                    orderedSnapShotProfilePeriods,
                    orderedRefreshProfilePeriods,
                    fundingLineId);     
            }
        }

        private void AdjustPeriodsForFundingAlreadyReleased(int variationPointerIndex,
            ProfilePeriod[] orderedSnapShotProfilePeriods,
            ProfilePeriod[] orderRefreshProfilePeriods)
        {
            for (int profilePeriod = 0; profilePeriod < variationPointerIndex; profilePeriod++)
            {
                orderRefreshProfilePeriods[profilePeriod].ProfiledValue = orderedSnapShotProfilePeriods[profilePeriod].ProfiledValue;
            }    
        }

        private void AdjustPeriodsForUnderPayment(int variationPointerIndex,
            decimal latestPeriodAmount,
            ProfilePeriod[] orderedSnapShotProfilePeriods,
            ProfilePeriod[] orderRefreshProfilePeriods)
        {
            decimal amountAlreadyPaid = orderedSnapShotProfilePeriods.Take(variationPointerIndex).Sum(_ => _.ProfiledValue);
            decimal amountThatShouldHaveBeenPaid = latestPeriodAmount * variationPointerIndex;

            decimal amountUnderPaid = Math.Abs(amountAlreadyPaid - amountThatShouldHaveBeenPaid);

            ProfilePeriod periodToAdjust = orderRefreshProfilePeriods[variationPointerIndex];

            periodToAdjust.ProfiledValue = periodToAdjust.ProfiledValue + amountUnderPaid;
        }
        
        private void AdjustPeriodsForNoTotalAllocationChange(int variationPointerIndex,
            decimal latestPeriodAmount,
            ProfilePeriod[] orderedSnapShotProfilePeriods,
            ProfilePeriod[] orderedRefreshProfilePeriods,
            string fundingLineId)
        {
            decimal amountAlreadyPaid = orderedSnapShotProfilePeriods.Take(variationPointerIndex).Sum(_ => _.ProfiledValue);
            decimal amountThatShouldHaveBeenPaid = latestPeriodAmount * variationPointerIndex;

            decimal runningTotalChange = amountThatShouldHaveBeenPaid - amountAlreadyPaid;

            if (runningTotalChange < 0)
            {
                AdjustingPeriodsForOverPaymentLeavesRemainder(variationPointerIndex,
                    latestPeriodAmount,
                    orderedSnapShotProfilePeriods,
                    orderedRefreshProfilePeriods, 
                    out decimal carryOver);

                if (carryOver > 0)
                {
                    RefreshState.AddCarryOver(fundingLineId, ProfilingCarryOverType.DSGReProfiling, carryOver);
                }
            }
            else if (runningTotalChange > 0)
            {
                AdjustPeriodsForUnderPayment(variationPointerIndex,
                    latestPeriodAmount,
                    orderedSnapShotProfilePeriods,
                    orderedRefreshProfilePeriods);
            }
        }

        private bool AdjustingPeriodsForOverPaymentLeavesRemainder(int variationPointerIndex,
            decimal latestPeriodAmount,
            ProfilePeriod[] orderedSnapShotProfilePeriods,
            ProfilePeriod[] orderRefreshProfilePeriods,
            out decimal remainingOverPayment)
        {
            decimal amountAlreadyPaid = orderedSnapShotProfilePeriods.Take(variationPointerIndex).Sum(_ => _.ProfiledValue);
            decimal amountThatShouldHaveBeenPaid = latestPeriodAmount * variationPointerIndex;

            remainingOverPayment = amountAlreadyPaid - amountThatShouldHaveBeenPaid;

            for (int profilePeriod = variationPointerIndex; profilePeriod < orderRefreshProfilePeriods.Length; profilePeriod++)
            {
                if (remainingOverPayment <= 0)
                {
                    break;
                }

                ProfilePeriod periodToAdjust = orderRefreshProfilePeriods[profilePeriod];

                decimal adjustedProfileValue = periodToAdjust.ProfiledValue - remainingOverPayment;

                if (adjustedProfileValue < 0)
                {
                    remainingOverPayment = Math.Abs(adjustedProfileValue);
                    adjustedProfileValue = 0;
                }
                else
                {
                    remainingOverPayment = 0;
                }

                periodToAdjust.ProfiledValue = adjustedProfileValue;
            }
            
            return remainingOverPayment != 0;
        }
    }
}