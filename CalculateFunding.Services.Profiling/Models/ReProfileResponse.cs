namespace CalculateFunding.Services.Profiling.Models
{
    public class ReProfileResponse
    {
        public DeliveryProfilePeriod[] DeliveryProfilePeriods { get; set; }

        public DistributionPeriods[] DistributionPeriods { get; set; }

        public string ProfilePatternKey { get; set; }

        public string ProfilePatternDisplayName { get; set; }

        public decimal CarryOverAmount { get; set; }

        public int? VariationPointerIndex { get; set; }

        public bool SkipReProfiling { get; set; }

        public string StrategyConfigKey { get; set; }

        public string Strategy { get; set; }
    }
}
