using CalculateFunding.Services.Profiling.Models;
using CalculateFunding.Services.Profiling.Tests.TestHelpers;

namespace CalculateFunding.Services.Profiling.Tests
{
    public class DeliveryProfilePeriodBuilder : TestEntityBuilder
    {
        private decimal _profiledValue;
        private string _typeValue;
        private int? _occurrence;
        private PeriodType? _periodType;
        private int? _year;
        private string _distributionPeriod;

        public DeliveryProfilePeriodBuilder WithProfiledValue(decimal value)
        {
            _profiledValue = value;

            return this;
        }

        public DeliveryProfilePeriodBuilder WithTypeValue(string typeValue)
        {
            _typeValue = typeValue;

            return this;
        }

        public DeliveryProfilePeriodBuilder WithOccurrence(int occurence)
        {
            _occurrence = occurence;

            return this;
        }

        public DeliveryProfilePeriodBuilder WithPeriodType(PeriodType periodType)
        {
            _periodType = periodType;

            return this;
        }

        public DeliveryProfilePeriodBuilder WithYear(int year)
        {
            _year = year;

            return this;
        }

        public DeliveryProfilePeriodBuilder WithDistributionPeriod(string distributionPeriod)
        {
            _distributionPeriod = distributionPeriod;

            return this;
        }
        
        public DeliveryProfilePeriod Build()
        {
            return new DeliveryProfilePeriod
            {
                ProfileValue   = _profiledValue,
                Type = _periodType.GetValueOrDefault(PeriodType.CalendarMonth),
                Occurrence = _occurrence.GetValueOrDefault(NewRandomNumberBetween(1, 10)),
                Year = _year.GetValueOrDefault(NewRandomYear()),
                DistributionPeriod = _distributionPeriod ?? NewRandomString(),
                TypeValue = _typeValue ?? NewRandomMonth()
            };
        }
    }
}