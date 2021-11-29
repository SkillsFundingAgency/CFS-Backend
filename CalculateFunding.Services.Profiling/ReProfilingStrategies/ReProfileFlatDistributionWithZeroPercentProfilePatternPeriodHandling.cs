using System;
using System.Linq;
using CalculateFunding.Services.Profiling.Models;

namespace CalculateFunding.Services.Profiling.ReProfilingStrategies
{
    public class ReProfileFlatDistributionWithZeroPercentProfilePatternPeriodHandling : FlatDistributionReProfilingStrategy, IReProfilingStrategy
    {
        public string StrategyKey => nameof(ReProfileFlatDistributionWithZeroPercentProfilePatternPeriodHandling);

        public string DisplayName => "Re-Profile Flat Distribution For Remaining Periods Handling 0% Profile Pattern Periods";

        public string Description => "Distributes changes to funding evenly across all of the remaining profile periods skipping any 0% Profile Pattern Periods unless all are 0%";

        public ReProfileStrategyResult ReProfile(ReProfileContext context)
        {
            ReProfileRequest reProfileRequest = context.Request;

            IProfilePeriod[] orderedRefreshProfilePeriods = new YearMonthOrderedProfilePeriods<IProfilePeriod>(context.ProfileResult.DeliveryProfilePeriods)
                .ToArray();
            IExistingProfilePeriod[] orderedExistingProfilePeriods = new YearMonthOrderedProfilePeriods<IExistingProfilePeriod>(reProfileRequest.ExistingPeriods)
                .ToArray();
            ProfilePeriodPattern[] orderedProfilePatternPeriods = new YearMonthOrderedProfilePatternPeriod(context.ProfilePattern.ProfilePattern)
                .ToArray();

            int variationPointerIndex = GetVariationPointerIndex(orderedRefreshProfilePeriods, orderedExistingProfilePeriods, context);
            bool shouldSkipZeroPercentPeriods = HasUnpaidNoneZeroProfilePeriods(orderedProfilePatternPeriods, variationPointerIndex);

            if (shouldSkipZeroPercentPeriods)
            {
                return FlatDistributionSkippingZeroPercentPeriods(orderedRefreshProfilePeriods,
                    orderedExistingProfilePeriods,
                    orderedProfilePatternPeriods,
                    reProfileRequest,
                    variationPointerIndex,
                    context);
            }

            return FlatDistribution(context,
                orderedRefreshProfilePeriods,
                orderedExistingProfilePeriods,
                reProfileRequest,
                variationPointerIndex);
        }

        private static ReProfileStrategyResult FlatDistributionSkippingZeroPercentPeriods(IProfilePeriod[] orderedRefreshProfilePeriods,
            IExistingProfilePeriod[] orderedExistingProfilePeriods,
            ProfilePeriodPattern[] orderedProfilePatternPeriods,
            ReProfileRequest reProfileRequest,
            int variationPointerIndex,
            ReProfileContext context)
        {
            RetainPaidProfilePeriodValues(variationPointerIndex, orderedExistingProfilePeriods, orderedRefreshProfilePeriods);
            DistributeRemainingFundingLineValueEvenlySkippingZeroPercentPeriods(orderedExistingProfilePeriods,
                variationPointerIndex,
                reProfileRequest,
                orderedRefreshProfilePeriods,
                orderedProfilePatternPeriods);

            decimal carryOverAmount = CalculateCarryOverAmount(reProfileRequest, orderedRefreshProfilePeriods);

            return new ReProfileStrategyResult
            {
                DistributionPeriods = MapIntoDistributionPeriods(context),
                DeliveryProfilePeriods = context.ProfileResult.DeliveryProfilePeriods,
                CarryOverAmount = carryOverAmount
            };
        }

        protected static void DistributeRemainingFundingLineValueEvenlySkippingZeroPercentPeriods(IExistingProfilePeriod[] orderedExistingProfilePeriods,
            int variationPointerIndex,
            ReProfileRequest reProfileRequest,
            IProfilePeriod[] orderedRefreshProfilePeriods,
            ProfilePeriodPattern[] orderedProfilePatternPeriods)
        {
            decimal previousFundingLineValuePaid = orderedExistingProfilePeriods.Take(variationPointerIndex).Sum(_ => _.GetProfileValue());
            decimal remainingFundingLineValueToPay = reProfileRequest.FundingLineTotal - previousFundingLineValuePaid;
            decimal remainingFundingLineProfiledToPay = orderedRefreshProfilePeriods.Skip(variationPointerIndex).Sum(_ => _.GetProfileValue());
            decimal differenceToDistribute = remainingFundingLineValueToPay - remainingFundingLineProfiledToPay;

            int finalNonZeroProfilePeriodIndex = orderedProfilePatternPeriods.ToList().FindLastIndex(_ => _.PeriodPatternPercentage > 0);

            MoveRoundingRemainderToFinalNoneZeroPercentPeriod(orderedRefreshProfilePeriods, finalNonZeroProfilePeriodIndex);
            DistributeRemainingBalanceSkippingZeroPercentPeriods(variationPointerIndex,
                orderedRefreshProfilePeriods,
                orderedExistingProfilePeriods,
                differenceToDistribute,
                orderedProfilePatternPeriods,
                finalNonZeroProfilePeriodIndex);
        }

        private static void MoveRoundingRemainderToFinalNoneZeroPercentPeriod(IProfilePeriod[] orderedRefreshProfilePeriods,
            int finalNonZeroProfilePeriodIndex)
        {
            IProfilePeriod finalRefreshPeriod = orderedRefreshProfilePeriods.Last();
            decimal finalProfiledPeriodValue = finalRefreshPeriod.GetProfileValue();

            if (finalProfiledPeriodValue > 0)
            {
                finalRefreshPeriod.SetProfiledValue(0);

                IProfilePeriod finalNoneZeroRefreshPeriod = orderedRefreshProfilePeriods[finalNonZeroProfilePeriodIndex];

                finalNoneZeroRefreshPeriod.SetProfiledValue(finalNoneZeroRefreshPeriod.GetProfileValue() + finalProfiledPeriodValue);
            }
        }

        protected static void DistributeRemainingBalanceSkippingZeroPercentPeriods(int variationPointerIndex,
            IProfilePeriod[] orderedRefreshProfilePeriods,
            IProfilePeriod[] orderedExistingProfilePeriods,
            decimal differenceToDistribute,
            ProfilePeriodPattern[] orderedProfilePatternPeriods,
            int finalNonZeroProfilePeriodIndex)
        {
            int remainingPeriodsToPay = orderedProfilePatternPeriods.Skip(variationPointerIndex).Count(_ => _.PeriodPatternPercentage > 0);

            decimal remainingPeriodsProfileValue = Math.Round(differenceToDistribute / remainingPeriodsToPay, 2, MidpointRounding.AwayFromZero);
            decimal remainderForFinalPeriod = differenceToDistribute - remainingPeriodsToPay * remainingPeriodsProfileValue;

            bool useExisting = false;

            if (differenceToDistribute == 0)
            {
                for (int refreshProfilePeriodIndex = variationPointerIndex; refreshProfilePeriodIndex < orderedRefreshProfilePeriods.Length; refreshProfilePeriodIndex++)
                {
                    ProfilePeriodPattern profilePeriodPattern = orderedProfilePatternPeriods[refreshProfilePeriodIndex];

                    if (profilePeriodPattern.PeriodPatternPercentage == 0M)
                    {
                        continue;
                    }

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
                ProfilePeriodPattern profilePeriodPattern = orderedProfilePatternPeriods[refreshProfilePeriodIndex];

                if (profilePeriodPattern.PeriodPatternPercentage == 0M)
                {
                    continue;
                }

                IProfilePeriod profilePeriod = orderedRefreshProfilePeriods[refreshProfilePeriodIndex];

                decimal adjustedProfileValue = useExisting ?
                    orderedExistingProfilePeriods[refreshProfilePeriodIndex].GetProfileValue() :
                    profilePeriod.GetProfileValue() + remainingPeriodsProfileValue;

                profilePeriod.SetProfiledValue(adjustedProfileValue);
            }

            
            if (differenceToDistribute == 0) return;

            IProfilePeriod finalProfilePeriod = orderedRefreshProfilePeriods[finalNonZeroProfilePeriodIndex];

            finalProfilePeriod.SetProfiledValue(finalProfilePeriod.GetProfileValue() + remainderForFinalPeriod);
        }

        private static bool HasUnpaidNoneZeroProfilePeriods(ProfilePeriodPattern[] orderedProfilePatternPeriods,
            int variationPointerIndex)
            => orderedProfilePatternPeriods.Skip(variationPointerIndex).Any(_ => _.PeriodPatternPercentage != 0M);
    }
}