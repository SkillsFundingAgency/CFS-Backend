using CalculateFunding.Services.Publishing.Profiling;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Publishing.UnitTests.Profiling
{
    public class ProfileTotalBuilder : TestEntityBuilder
    {
        private string _typeValue;
        private int? _occurrence;
        private int? _year;
        private decimal? _value;

        public ProfileTotalBuilder WithTypeValue(string typeValue)
        {
            _typeValue = typeValue;

            return this;
        }

        public ProfileTotalBuilder WithOccurrence(int occurrence)
        {
            _occurrence = occurrence;

            return this;
        }

        public ProfileTotalBuilder WithYear(int year)
        {
            _year = year;

            return this;
        }

        public ProfileTotalBuilder WithValue(decimal value)
        {
            _value = value;

            return this;
        }
        
        public ProfileTotal Build()
        {
            return new ProfileTotal
            {
                Year = _year.GetValueOrDefault(NewRandomYear()),
                Occurrence = _occurrence.GetValueOrDefault(NewRandomNumberBetween(0, 3)),
                Value = _value.GetValueOrDefault(NewRandomNumberBetween(1000, 99999)),
                TypeValue = _typeValue ?? NewRandomMonth()
            };
        }     
    }
}