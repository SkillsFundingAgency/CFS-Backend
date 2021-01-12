using CalculateFunding.Services.Profiling.Models;
using CalculateFunding.Services.Profiling.Tests.TestHelpers;

namespace CalculateFunding.Services.Profiling.Tests
{
    public class AllocationProfileResponseBuilder : TestEntityBuilder
    {
        private string _profilePatternKey;
        private string _profilePatternDisplayName;
        private DeliveryProfilePeriod[] _deliveryProfilePeriods;
        private DistributionPeriods[] _distributionPeriods;

        public AllocationProfileResponseBuilder WithProfilePatternKey(string profilePatternKey)
        {
            _profilePatternKey = profilePatternKey;

            return this;
        }

        public AllocationProfileResponseBuilder WithProfilePatternDisplayName(string profilePatternDisplayName)
        {
            _profilePatternDisplayName = profilePatternDisplayName;

            return this;
        }

        public AllocationProfileResponseBuilder WithDeliveryProfilePeriods(params DeliveryProfilePeriod[] deliveryProfilePeriods)
        {
            _deliveryProfilePeriods = deliveryProfilePeriods;

            return this;
        }

        public AllocationProfileResponseBuilder WithProfilePeriods(params DistributionPeriods[] distributionPeriods)
        {
            _distributionPeriods = distributionPeriods;

            return this;
        }

        public AllocationProfileResponse Build() => new AllocationProfileResponse
        {
            DeliveryProfilePeriods = _deliveryProfilePeriods ?? new DeliveryProfilePeriod[0],
            DistributionPeriods = _distributionPeriods ?? new DistributionPeriods[0],
            ProfilePatternKey = _profilePatternKey ?? NewRandomString(),
            ProfilePatternDisplayName = _profilePatternDisplayName ?? NewRandomString()
        };
    }
}