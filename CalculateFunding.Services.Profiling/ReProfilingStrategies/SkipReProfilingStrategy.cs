using CalculateFunding.Services.Profiling.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Profiling.ReProfilingStrategies
{
    public class SkipReProfilingStrategy : ReProfilingStrategy, IReProfilingStrategy
    {
        public string StrategyKey => nameof(SkipReProfilingStrategy);

        public string DisplayName => "Used to skip reprofiling for targeted strategy.";

        public string Description => "Used to skip reprofiling for targeted strategy.";

        public ReProfileStrategyResult ReProfile(ReProfileContext context)
        {
            ReProfileRequest reProfileRequest = context.Request;

            IProfilePeriod[] orderedRefreshProfilePeriods = new YearMonthOrderedProfilePeriods<IProfilePeriod>(context.ProfileResult.DeliveryProfilePeriods)
                .ToArray();
            IExistingProfilePeriod[] orderedAllExistingProfilePeriods = new YearMonthOrderedProfilePeriods<IExistingProfilePeriod>(reProfileRequest.AllExistingPeriods)
                .ToArray();

            for (int profilePeriodIndex = 0; profilePeriodIndex < orderedAllExistingProfilePeriods.Length; profilePeriodIndex++)
            {
                IProfilePeriod existingProfilePeriod = orderedAllExistingProfilePeriods[profilePeriodIndex];

                if (orderedRefreshProfilePeriods.Length > profilePeriodIndex)
                {
                    IProfilePeriod refreshProfilePeriod = orderedRefreshProfilePeriods[profilePeriodIndex];

                    refreshProfilePeriod.SetProfiledValue(existingProfilePeriod.GetProfileValue());
                }
            };

            return new ReProfileStrategyResult
            {
                DistributionPeriods = MapIntoDistributionPeriods(context),
                DeliveryProfilePeriods = context.ProfileResult.DeliveryProfilePeriods,
                CarryOverAmount = reProfileRequest.FundingLineTotal - orderedRefreshProfilePeriods.Sum(_ => _.GetProfileValue())
            };
        }
    }
}
