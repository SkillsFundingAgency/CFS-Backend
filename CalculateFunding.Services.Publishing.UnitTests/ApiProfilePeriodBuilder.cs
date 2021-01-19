using CalculateFunding.Common.ApiClient.Profiling.Models;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Publishing.UnitTests
{
    public class ApiProfilePeriodBuilder : TestEntityBuilder
    {
        private int _occurrence;
        private string _period;
        private string _type;
        private decimal _value;
        private string _distributionPeriod;
        private int _year;

        public ApiProfilePeriodBuilder WithOccurrence(int occurrence)
        {
            _occurrence = occurrence;

            return this;
        }

        public ApiProfilePeriodBuilder WithPeriod(string period)
        {
            _period = period;

            return this;
        }

        public ApiProfilePeriodBuilder WithType(string type)
        {
            _type = type;

            return this;
        }

        public ApiProfilePeriodBuilder WithValue(decimal value)
        {
            _value = value;

            return this;
        }

        public ApiProfilePeriodBuilder WithDistributionPeriod(string distributionPeriod)
        {
            _distributionPeriod = distributionPeriod;

            return this;
        }

        public ApiProfilePeriodBuilder WithYear(int year)
        {
            _year = year;

            return this;
        }
        
        public ProfilingPeriod Build()
        {
            return new ProfilingPeriod
            {
                Occurrence = _occurrence,
                Period = _period,
                Type = _type,
                Value = _value,
                Year = _year,
                DistributionPeriod = _distributionPeriod
            };
        }
    }
}