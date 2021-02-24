using System;
using System.Linq;
using CalculateFunding.Services.Profiling.Models;

namespace CalculateFunding.Services.Profiling.ReProfilingStrategies
{
    public abstract class ReProfilingStrategy
    {
        protected static DistributionPeriods[] MapIntoDistributionPeriods(ReProfileContext context)
        {
            return context.ProfileResult.DeliveryProfilePeriods.GroupBy(_ => _.DistributionPeriod)
                .Select(_ => new DistributionPeriods
                {
                    Value = _.Sum(dp => dp.ProfileValue),
                    DistributionPeriodCode = _.Key
                })
                .ToArray();
        }

        protected int GetVariationPointerIndex(IProfilePeriod[] orderedRefreshProfilePeriods,
            IExistingProfilePeriod[] orderedExistingProfilePeriods)
        {
            IExistingProfilePeriod finalPaidProfilePeriod = orderedExistingProfilePeriods.LastOrDefault(_ => _.IsPaid);
            
            return finalPaidProfilePeriod == null ? 0 : Array.IndexOf(orderedExistingProfilePeriods, finalPaidProfilePeriod) + 1;    
        }
    }
}