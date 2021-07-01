using System.Linq;

namespace CalculateFunding.Services.Profiling.Models
{
    public class ReProfileStrategyResult
    {
        public DeliveryProfilePeriod[] DeliveryProfilePeriods { get; set; }

        public DistributionPeriods[] DistributionPeriods { get; set; }

        public DeliveryProfilePeriod[] NegativeDeliveryProfilePeriods => DeliveryProfilePeriods.Select(_ => new DeliveryProfilePeriod { 
            DistributionPeriod = _.DistributionPeriod,
            Occurrence = _.Occurrence,
            ProfileValue = _.ProfileValue * -1,
            Type = _.Type,
            TypeValue = _.TypeValue,
            Year = _.Year
        }).ToArray();

        public DistributionPeriods[] NegativeDistributionPeriods => DistributionPeriods.Select(_ => new DistributionPeriods { 
            DistributionPeriodCode = _.DistributionPeriodCode, 
            Value = _.Value * -1 
        }).ToArray();

        public decimal CarryOverAmount { get; set; }

        public decimal NegativeCarryOverAmount => CarryOverAmount * -1;
    }
}
