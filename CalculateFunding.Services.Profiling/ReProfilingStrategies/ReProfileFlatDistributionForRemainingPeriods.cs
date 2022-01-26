using System.Linq;
using CalculateFunding.Services.Profiling.Models;

namespace CalculateFunding.Services.Profiling.ReProfilingStrategies
{
    public class ReProfileFlatDistributionForRemainingPeriods : FlatDistributionReProfilingStrategy, IReProfilingStrategy
    {
        public virtual string StrategyKey => nameof(ReProfileFlatDistributionForRemainingPeriods);

        public virtual string DisplayName => "Re-Profile Flat Distribution For Remaining Periods";

        public virtual string Description => "Distributes changes to funding evenly across all of the remaining profile periods";
        
        public ReProfileStrategyResult ReProfile(ReProfileContext context)
        {
            ReProfileRequest reProfileRequest = context.Request;

            IProfilePeriod[] orderedRefreshProfilePeriods = new YearMonthOrderedProfilePeriods<IProfilePeriod>(context.ProfileResult.DeliveryProfilePeriods)
                .ToArray();
            IExistingProfilePeriod[] orderedExistingProfilePeriods = new YearMonthOrderedProfilePeriods<IExistingProfilePeriod>(reProfileRequest.ExistingPeriods)
                .ToArray();

            int variationPointerIndex = GetVariationPointerIndex(orderedRefreshProfilePeriods,
                orderedExistingProfilePeriods,
                context);

            variationPointerIndex = context.Request.AlreadyPaidUpToIndex ?
                        variationPointerIndex - 1 : 
                        variationPointerIndex;

            return FlatDistribution(context,
                orderedRefreshProfilePeriods,
                orderedExistingProfilePeriods,
                reProfileRequest,
                variationPointerIndex,
                DistributeRemainingFundingLineValueEvenly);
        }
    }
}