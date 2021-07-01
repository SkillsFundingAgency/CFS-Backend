using System.Linq;

namespace CalculateFunding.Services.Profiling.Models
{
    public class ReProfileStrategyResult
    {
        public DeliveryProfilePeriod[] DeliveryProfilePeriods { get; set; }

        public DistributionPeriods[] DistributionPeriods { get; set; }

        public decimal CarryOverAmount { get; set; }
    }
}
