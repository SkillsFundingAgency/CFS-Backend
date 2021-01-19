namespace CalculateFunding.Services.Profiling.Models
{
    public class BatchAllocationProfileResponse : AllocationProfileResponse
    {
        public BatchAllocationProfileResponse()
        {
        }

        public BatchAllocationProfileResponse(string key,
            decimal fundingValue, AllocationProfileResponse allocationProfileResponse)
            : base(allocationProfileResponse.DeliveryProfilePeriods,
                allocationProfileResponse.DistributionPeriods)
        {
            ProfilePatternKey = allocationProfileResponse.ProfilePatternKey;
            ProfilePatternDisplayName = allocationProfileResponse.ProfilePatternDisplayName;

            Key = key;
            FundingValue = fundingValue;
        }
        
        public string Key { get; set; }
        
        public decimal FundingValue { get; set; }
    }
}