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
            IExistingProfilePeriod[] orderedExistingProfilePeriods,
            ReProfileContext context)
        {
            int? requestVariationPointerIndex = context.Request?.VariationPointerIndex;
            
            if (requestVariationPointerIndex.HasValue)
            {
                return requestVariationPointerIndex.GetValueOrDefault();
            }
            
            IExistingProfilePeriod finalPaidProfilePeriod = orderedExistingProfilePeriods.LastOrDefault(_ => _.IsPaid);
            
            return finalPaidProfilePeriod == null ? 0 : Array.IndexOf(orderedExistingProfilePeriods, finalPaidProfilePeriod) + 1;    
        }

        protected static void ZeroPaidProfilePeriodValues(int variationPointerIndex,
            IProfilePeriod[] orderedRefreshProfilePeriods,
            IProfilePeriod[] orderedExistingProfilePeriods = null)
        {
            for (int paidProfilePeriodIndex = 0; paidProfilePeriodIndex < variationPointerIndex; paidProfilePeriodIndex++)
            {
                IProfilePeriod refreshProfilePeriod = orderedRefreshProfilePeriods[paidProfilePeriodIndex];
                refreshProfilePeriod.SetProfiledValue(0);

                if (orderedExistingProfilePeriods != null)
                {
                    IProfilePeriod existingProfilePeriod = orderedExistingProfilePeriods[paidProfilePeriodIndex];
                    existingProfilePeriod.SetProfiledValue(0);
                }
            }
        }

        protected static void ZeroRemainingPeriodValues(int variationPointerIndex,
            IProfilePeriod[] orderedRefreshProfilePeriods)
        {
            for (int futureProfilePeriodIndex = variationPointerIndex; futureProfilePeriodIndex < orderedRefreshProfilePeriods.Length; futureProfilePeriodIndex++)
            {
                IProfilePeriod refreshProfilePeriod = orderedRefreshProfilePeriods[futureProfilePeriodIndex];

                refreshProfilePeriod.SetProfiledValue(0);
            }
        }

        protected static void RetainPaidProfilePeriodValues(int variationPointerIndex,
            IExistingProfilePeriod[] orderedExistingProfilePeriods,
            IProfilePeriod[] orderedRefreshProfilePeriods)
        {
            for (int paidProfilePeriodIndex = 0; paidProfilePeriodIndex < variationPointerIndex; paidProfilePeriodIndex++)
            {
                IProfilePeriod paidProfilePeriod = orderedExistingProfilePeriods[paidProfilePeriodIndex];
                IProfilePeriod refreshProfilePeriod = orderedRefreshProfilePeriods[paidProfilePeriodIndex];

                refreshProfilePeriod.SetProfiledValue(paidProfilePeriod.GetProfileValue());
            }
        }
    }
}