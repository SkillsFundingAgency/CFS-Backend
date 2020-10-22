using CalculateFunding.Common.ApiClient.Profiling.Models;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Publishing.UnitTests.Profiling
{
    public class DeliveryProfilePeriodBuilder : TestEntityBuilder
    {
        private string _distributionPeriod;
        private int? _occurrence;
        private decimal? _value;
        private PeriodType? _periodType;
        private int? _year;
        private string _typeValue;

        public DeliveryProfilePeriodBuilder WithDistributionPeriod(string distributionPeriod)
        {
            _distributionPeriod = distributionPeriod;

            return this;
        }

        public DeliveryProfilePeriodBuilder WithOccurrence(int occurrence)
        {
            _occurrence = occurrence;

            return this;
        }

        public DeliveryProfilePeriodBuilder WithValue(decimal? value)
        {
            _value = value;

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

        public DeliveryProfilePeriodBuilder WithTypeValue(string typeValue)
        {
            _typeValue = typeValue;

            return this;
        }
        
        public DeliveryProfilePeriod Build()
        {
            return new DeliveryProfilePeriod
            {
                DistributionPeriod   = _distributionPeriod ?? NewRandomString(),
                Occurrence = _occurrence.GetValueOrDefault(NewRandomNumberBetween(0, 10)),
                ProfileValue = _value.GetValueOrDefault(NewRandomNumberBetween(999, int.MaxValue)),
                Type = _periodType.GetValueOrDefault(PeriodType.CalendarMonth),
                Year = _year.GetValueOrDefault(NewRandomYear()),
                TypeValue = _typeValue ?? NewRandomMonth()
            };
        }
    }
}