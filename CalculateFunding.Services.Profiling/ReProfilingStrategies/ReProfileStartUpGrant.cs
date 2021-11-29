using System;
using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Services.Profiling.Models;

namespace CalculateFunding.Services.Profiling.ReProfilingStrategies
{
    public class ReProfileStartUpGrant : ReProfilingStrategy, IReProfilingStrategy
    {
        public string StrategyKey => nameof(ReProfileStartUpGrant);

        public string DisplayName => "Re-Profile GAG Start Up Grant";

        public string Description => "Distribute start-up grants got Academies";

        public ReProfileStrategyResult ReProfile(ReProfileContext context)
        {
            IProfilePeriod[] orderedRefreshProfilePeriods = new YearMonthOrderedProfilePeriods<IProfilePeriod>(context.ProfileResult.DeliveryProfilePeriods)
                .ToArray();
            IExistingProfilePeriod[] orderedExistingProfilePeriods = new YearMonthOrderedProfilePeriods<IExistingProfilePeriod>(context.Request.ExistingPeriods)
                .ToArray();

            int variationPointerIndex = GetVariationPointerIndex(orderedRefreshProfilePeriods, orderedExistingProfilePeriods, context);

            decimal[] grantPaymentDistribution = GetStartUpGrantDistribution(orderedRefreshProfilePeriods.Length, variationPointerIndex, context.Request.FundingLineTotal);

            ZeroPaidProfilePeriodValues(variationPointerIndex, orderedRefreshProfilePeriods);
            DistributeGrantPayments(variationPointerIndex, orderedRefreshProfilePeriods, grantPaymentDistribution);

            return new ReProfileStrategyResult
            {
                DistributionPeriods = MapIntoDistributionPeriods(context),
                DeliveryProfilePeriods = context.ProfileResult.DeliveryProfilePeriods,
                CarryOverAmount = 0
            };
        }

        private static void DistributeGrantPayments(int variationPointerIndex,
            IProfilePeriod[] orderedRefreshProfilePeriods,
            decimal[] grantPaymentDistribution)
        {
            Queue<decimal> grantPayments = new Queue<decimal>(grantPaymentDistribution);

            for (int remainingProfileIndex = variationPointerIndex; remainingProfileIndex < orderedRefreshProfilePeriods.Length; remainingProfileIndex++)
            {
                decimal profileAmount = grantPayments.TryDequeue(out decimal grantPayment) ? grantPayment : 0M;

                orderedRefreshProfilePeriods[remainingProfileIndex].SetProfiledValue(profileAmount);
            }
        }

        private decimal[] GetStartUpGrantDistribution(int periodsLength,
            int variationPointerIndex,
            decimal totalAllocation)
        {
            int remainingPeriodCount = periodsLength - variationPointerIndex;

            if (remainingPeriodCount >= 3)
            {
                decimal fiftyPercent = totalAllocation * .5M;
                decimal twentyFivePercent = totalAllocation * .25M;
                decimal fiftyPercentRoundedDown = RoundDownTo2Dp(fiftyPercent);
                decimal twentyFivePercentRoundedDown = RoundDownTo2Dp(twentyFivePercent);
                decimal finalGrantPayment = twentyFivePercent + (fiftyPercent - fiftyPercentRoundedDown) + (twentyFivePercent - twentyFivePercentRoundedDown);

                return new[]
                {
                    fiftyPercentRoundedDown,
                    twentyFivePercentRoundedDown,
                    finalGrantPayment
                };
            }

            if (remainingPeriodCount == 2)
            {
                decimal fiftyPercent = totalAllocation * .5M;
                decimal fiftyPercentRoundedDown = RoundDownTo2Dp(fiftyPercent);
                decimal finalGrantPayment = fiftyPercent + (fiftyPercent - fiftyPercentRoundedDown);

                return new[]
                {
                    fiftyPercentRoundedDown,
                    finalGrantPayment
                };
            }

            return new[]
            {
                totalAllocation
            };
        }

        private static decimal RoundDownTo2Dp(decimal initialValue) => Math.Round(initialValue, 2, MidpointRounding.ToZero);
    }
}