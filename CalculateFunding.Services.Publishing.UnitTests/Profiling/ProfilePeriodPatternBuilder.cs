using CalculateFunding.Models.Publishing;
using CalculateFunding.Tests.Common.Helpers;
using CalculateFunding.Common.ApiClient.Profiling.Models;

namespace CalculateFunding.Services.Publishing.UnitTests.Profiling
{
    public class ProfilePeriodPatternBuilder : TestEntityBuilder
    {
        private string _typeValue;
        private PeriodType? _type;
        private int? _year;
        private int? _occurence;
        private string _distributionPeriodId;

        public ProfilePeriodPatternBuilder WithOccurence(int occurence)
        {
            _occurence = occurence;

            return this;
        }

        public ProfilePeriodPatternBuilder WithTypeValue(string typeValue)
        {
            _typeValue = typeValue;

            return this;
        }

        public ProfilePeriodPatternBuilder WithType(PeriodType type)
        {
            _type = type;

            return this;
        }

        public ProfilePeriodPatternBuilder WithYear(int year)
        {
            _year = year;

            return this;
        }

        public ProfilePeriodPatternBuilder WithDistributionPeriodId(string distributionPeriodId)
        {
            _distributionPeriodId = distributionPeriodId;

            return this;
        }

        public ProfilePeriodPattern Build()
        {
            return new ProfilePeriodPattern
            {
                PeriodType = _type.GetValueOrDefault(NewRandomEnum<PeriodType>()),
                Period = _typeValue ?? NewRandomMonth(),
                PeriodYear = _year.GetValueOrDefault(NewRandomDateTime().Year),
                Occurrence = _occurence.GetValueOrDefault(NewRandomNumberBetween(0, 2)),
                DistributionPeriod = _distributionPeriodId ?? NewRandomString()
            };
        }
    }
}