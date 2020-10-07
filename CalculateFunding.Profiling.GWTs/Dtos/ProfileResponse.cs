namespace CalculateFunding.Profiling.GWTs.Dtos
{
    public class ProfileResponse
    {
        public ProfileResponse(ProfileRequest allocationProfileRequest, DeliveryProfilePeriod[] deliveryProfilePeriods)
        {
            AllocationProfileRequest = allocationProfileRequest;
            DeliveryProfilePeriods = deliveryProfilePeriods;
        }

        public ProfileRequest AllocationProfileRequest { get; }

        public DeliveryProfilePeriod[] DeliveryProfilePeriods { get; }
    }
}