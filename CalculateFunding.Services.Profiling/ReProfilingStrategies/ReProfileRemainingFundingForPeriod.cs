using System.Linq;
using CalculateFunding.Services.Profiling.Models;

namespace CalculateFunding.Services.Profiling.ReProfilingStrategies
{
    public class ReProfileRemainingFundingForPeriod : ReProfilingStrategy, IReProfilingStrategy
    {
        public string StrategyKey => nameof(ReProfileRemainingFundingForPeriod);

        public string DisplayName => "Re-Profile Remaining Funding For Period";

        public string Description => "Profiles Funding Lines for either new openers mid period of new allocations mid period to distribute the pro rata allocation remaining for that period";

        public ReProfileStrategyResult ReProfile(ReProfileContext context)
        {
            ReProfileRequest reProfileRequest = context.Request;
            
            IProfilePeriod[] orderedRefreshProfilePeriods = new YearMonthOrderedProfilePeriods<IProfilePeriod>(context.ProfileResult.DeliveryProfilePeriods)
                .ToArray();
            IExistingProfilePeriod[] orderedExistingProfilePeriods = new YearMonthOrderedProfilePeriods<IExistingProfilePeriod>(reProfileRequest.ExistingPeriods)
                .ToArray();

            int variationPointerIndex = GetVariationPointerIndex(orderedRefreshProfilePeriods, orderedExistingProfilePeriods, context);

            decimal carryOverUnpaidValues = 0;

            for (int refreshProfilePeriodIndex = 0; refreshProfilePeriodIndex < variationPointerIndex; refreshProfilePeriodIndex++)
            {
                carryOverUnpaidValues += orderedRefreshProfilePeriods[refreshProfilePeriodIndex].GetProfileValue();
                orderedRefreshProfilePeriods[refreshProfilePeriodIndex].SetProfiledValue(0);
            }

            int flatSplitValue = (int)carryOverUnpaidValues / (orderedRefreshProfilePeriods.Length - variationPointerIndex);
            decimal remainder = carryOverUnpaidValues % (orderedRefreshProfilePeriods.Length - variationPointerIndex);

            orderedRefreshProfilePeriods.Skip(variationPointerIndex).ForEach(_ =>
            {
                _.SetProfiledValue(_.GetProfileValue() + flatSplitValue);
            });

            orderedRefreshProfilePeriods.Last().SetProfiledValue(orderedRefreshProfilePeriods.Last().GetProfileValue() + remainder);

            return new ReProfileStrategyResult
            {
                DistributionPeriods = MapIntoDistributionPeriods(context),
                DeliveryProfilePeriods = context.ProfileResult.DeliveryProfilePeriods,
                CarryOverAmount = 0
            };
        }
    }
}