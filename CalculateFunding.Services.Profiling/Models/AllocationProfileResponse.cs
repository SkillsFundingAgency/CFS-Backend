namespace CalculateFunding.Services.Profiling.Models
{
    public class AllocationProfileResponse
    {
        public AllocationProfileResponse()
        {

        }

        public AllocationProfileResponse(DeliveryProfilePeriod[] deliveryProfilePeriods, DistributionPeriods[] distributionPeriods)
        {

            DeliveryProfilePeriods = deliveryProfilePeriods;
            DistributionPeriods = distributionPeriods;
        }
        public DeliveryProfilePeriod[] DeliveryProfilePeriods { get; set; }
        public DistributionPeriods[] DistributionPeriods { get; set; }

        public string ProfilePatternKey { get; set; }

        public string ProfilePatternDisplayName { get; set; }
    }
}