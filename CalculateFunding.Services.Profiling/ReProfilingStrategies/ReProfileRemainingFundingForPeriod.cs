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

            int variationPointerIndex = GetVariationPointerIndex(orderedRefreshProfilePeriods, orderedExistingProfilePeriods);

            for (int refreshProfilePeriodIndex = 0; refreshProfilePeriodIndex < variationPointerIndex; refreshProfilePeriodIndex++)
            {
                orderedRefreshProfilePeriods[refreshProfilePeriodIndex].SetProfiledValue(0);
            }
            
            return new ReProfileStrategyResult
            {
                DistributionPeriods = MapIntoDistributionPeriods(context),
                DeliveryProfilePeriods = context.ProfileResult.DeliveryProfilePeriods,
                CarryOverAmount = 0
            };
        }
    }
}