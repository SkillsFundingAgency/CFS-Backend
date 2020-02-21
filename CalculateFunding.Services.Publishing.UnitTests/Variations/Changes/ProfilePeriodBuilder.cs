using CalculateFunding.Models.Publishing;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Publishing.UnitTests.Variations.Changes
{
    public class ProfilePeriodBuilder : TestEntityBuilder
    {
        private string _typeValue;
        private ProfilePeriodType? _type;
        private decimal? _profiledAmount;
        private int? _year;
        private int? _occurence;

        public ProfilePeriodBuilder WithOccurence(int occurence)
        {
            _occurence = occurence;

            return this;
        }

        public ProfilePeriodBuilder WithTypeValue(string typeValue)
        {
            _typeValue = typeValue;

            return this;
        }

        public ProfilePeriodBuilder WithType(ProfilePeriodType type)
        {
            _type = type;

            return this;
        }

        public ProfilePeriodBuilder WithAmount(decimal amount)
        {
            _profiledAmount = amount;

            return this;
        }

        public ProfilePeriodBuilder WithYear(int year)
        {
            _year = year;

            return this;
        }
        
        public ProfilePeriod Build()
        {
            return new ProfilePeriod
            {
                Type = _type.GetValueOrDefault(NewRandomEnum<ProfilePeriodType>()),
                TypeValue = _typeValue ?? NewRandomMonth(),
                ProfiledValue = _profiledAmount.GetValueOrDefault(NewRandomNumberBetween(1900, 3000)),
                Year = _year.GetValueOrDefault(NewRandomDateTime().Year),
                Occurrence = _occurence.GetValueOrDefault(NewRandomNumberBetween(0, 2))
            };
        }
    }
}