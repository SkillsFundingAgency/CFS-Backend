using CalculateFunding.Common.ApiClient.Profiling.Models;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Publishing.UnitTests.Profiling
{
    public class ExistingProfilePeriodBuilder : TestEntityBuilder
    {
        private string _distributionPeriod;
        private int? _occurrence;
        private decimal? _value;
        private PeriodType? _periodType;
        private int? _year;
        private string _typeValue;

        public ExistingProfilePeriodBuilder WithDistributionPeriod(string distributionPeriod)
        {
            _distributionPeriod = distributionPeriod;

            return this;
        }

        public ExistingProfilePeriodBuilder WithOccurrence(int occurrence)
        {
            _occurrence = occurrence;

            return this;
        }

        public ExistingProfilePeriodBuilder WithValue(decimal? value)
        {
            _value = value;

            return this;
        }

        public ExistingProfilePeriodBuilder WithPeriodType(PeriodType periodType)
        {
            _periodType = periodType;

            return this;
        }

        public ExistingProfilePeriodBuilder WithYear(int year)
        {
            _year = year;

            return this;
        }

        public ExistingProfilePeriodBuilder WithTypeValue(string typeValue)
        {
            _typeValue = typeValue;

            return this;
        }
        
        public ExistingProfilePeriod Build()
        {
            return new ExistingProfilePeriod
            {
                DistributionPeriod   = _distributionPeriod ?? NewRandomString(),
                Occurrence = _occurrence.GetValueOrDefault(NewRandomNumberBetween(0, 10)),
                ProfileValue        = _value,
                Type = _periodType.GetValueOrDefault(PeriodType.CalendarMonth),
                Year = _year.GetValueOrDefault(NewRandomYear()),
                TypeValue = _typeValue ?? NewRandomMonth()
            };
        }
    }
}