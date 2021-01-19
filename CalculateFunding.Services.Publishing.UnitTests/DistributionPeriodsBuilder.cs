using CalculateFunding.Common.ApiClient.Profiling.Models;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Publishing.UnitTests
{
    public class DistributionPeriodsBuilder : TestEntityBuilder
    {
        private decimal _value;
        private string _distributionPeriodCode;

        public DistributionPeriodsBuilder WithValue(decimal value)
        {
            _value = value;

            return this;
        }

        public DistributionPeriodsBuilder WithDistributionPeriodCode(string distributionPeriodCode)
        {
            _distributionPeriodCode = distributionPeriodCode;

            return this;
        }
        
        public DistributionPeriods Build()
        {
            return new DistributionPeriods
            {
                Value = _value,
                DistributionPeriodCode = _distributionPeriodCode
            };
        }
    }
}