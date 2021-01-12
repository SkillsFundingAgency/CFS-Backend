namespace CalculateFunding.Services.Profiling.Models
{
    public class BatchAllocationProfileResponse : AllocationProfileResponse
    {
        public BatchAllocationProfileResponse()
        {
        }

        public BatchAllocationProfileResponse(decimal fundingValue, AllocationProfileResponse allocationProfileResponse)
            : base(allocationProfileResponse.DeliveryProfilePeriods,
                allocationProfileResponse.DistributionPeriods)
        {
            ProfilePatternKey = allocationProfileResponse.ProfilePatternKey;
            ProfilePatternDisplayName = allocationProfileResponse.ProfilePatternDisplayName;

            FundingValue = fundingValue;
        }
        
        public decimal FundingValue { get; set; }
    }
}