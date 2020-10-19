using CalculateFunding.Services.Profiling.Models;
using CalculateFunding.Services.Profiling.Tests.TestHelpers;

namespace CalculateFunding.Services.Profiling.Tests.ReProfilingStrategies
{
    public class ExistingProfilePeriodBuilder : TestEntityBuilder
    {
        private string _typeValue;
        private int? _occurrence;
        private PeriodType? _periodType;
        private int? _year;
        private decimal? _profiledValue;
        private string _distributionPeriod;

        public ExistingProfilePeriodBuilder WithTypeValue(string typeValue)
        {
            _typeValue = typeValue;

            return this;
        }

        public ExistingProfilePeriodBuilder WithOccurrence(int occurence)
        {
            _occurrence = occurence;

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

        public ExistingProfilePeriodBuilder WithProfiledValue(decimal profiledValue)
        {
            _profiledValue = profiledValue;

            return this;
        }

        public ExistingProfilePeriodBuilder WithDistributionPeriod(string distributionPeriod)
        {
            _distributionPeriod = distributionPeriod;

            return this;
        }
        
        public ExistingProfilePeriod Build()
        {
            return new ExistingProfilePeriod
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