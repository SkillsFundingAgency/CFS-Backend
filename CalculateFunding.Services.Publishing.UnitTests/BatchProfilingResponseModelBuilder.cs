using CalculateFunding.Common.ApiClient.Profiling.Models;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Publishing.UnitTests
{
    public class BatchProfilingResponseModelBuilder : TestEntityBuilder
    {
        private string _key;
        private DistributionPeriods[] _distributionPeriods;
        private decimal _fundingValue;
        private ProfilingPeriod[] _deliveryProfilePeriods;
        private string _profilePatternKey;
        private string _profilePatternDisplayName;

        public BatchProfilingResponseModelBuilder WithKey(string key)
        {
            _key = key;

            return this;
        }
        
        public BatchProfilingResponseModelBuilder WithDistributionPeriods(params DistributionPeriods[] distributionPeriods)
        {
            _distributionPeriods = distributionPeriods;

            return this;
        }
        
        public BatchProfilingResponseModelBuilder WithFundingValue(decimal fundingValue)
        {
            _fundingValue = fundingValue;

            return this;
        }
        
        public BatchProfilingResponseModelBuilder WithDeliveryProfilePeriods(params ProfilingPeriod[] profilePeriods)
        {
            _deliveryProfilePeriods = profilePeriods;

            return this;
        }
        
        public BatchProfilingResponseModelBuilder WithProfilePatternKey(string profilePatternKey)
        {
            _profilePatternKey = profilePatternKey;

            return this;
        }
        
        public BatchProfilingResponseModelBuilder WithProfilePatternDisplayName(string profilePatternDisplayName)
        {
            _profilePatternDisplayName = profilePatternDisplayName;

            return this;
        }
        
        public BatchProfilingResponseModel Build()
        {
            return new BatchProfilingResponseModel
            {
                Key = _key,
                DistributionPeriods = _distributionPeriods,
                FundingValue = _fundingValue,
                DeliveryProfilePeriods = _deliveryProfilePeriods,
                ProfilePatternKey = _profilePatternKey,
                ProfilePatternDisplayName = _profilePatternDisplayName
            };
        }
    }
}