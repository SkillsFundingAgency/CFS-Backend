using CalculateFunding.Services.Profiling.Models;
using CalculateFunding.Services.Profiling.Tests.TestHelpers;

namespace CalculateFunding.Services.Profiling.Tests
{
    public class DistributionPeriodsBuilder : TestEntityBuilder
    {
        private decimal _value;

        public DistributionPeriodsBuilder WithValue(decimal value)
        {
            _value = value;

            return this;
        }
        
        public DistributionPeriods Build()
        {
            return new DistributionPeriods
            {
                Value = _value
            };
        }
    }
}