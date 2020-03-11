using CalculateFunding.Models.Publishing;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Publishing.UnitTests.Profiling
{
    public class FundingStreamPaymentDateBuilder : TestEntityBuilder
    {
        private string _typeValue;
        private ProfilePeriodType? _type;
        private int? _year;
        private int? _occurence;

        public FundingStreamPaymentDateBuilder WithOccurence(int occurence)
        {
            _occurence = occurence;

            return this;
        }

        public FundingStreamPaymentDateBuilder WithTypeValue(string typeValue)
        {
            _typeValue = typeValue;

            return this;
        }

        public FundingStreamPaymentDateBuilder WithType(ProfilePeriodType type)
        {
            _type = type;

            return this;
        }

        public FundingStreamPaymentDateBuilder WithYear(int year)
        {
            _year = year;

            return this;
        }
        
        public FundingStreamPaymentDate Build()
        {
            return new FundingStreamPaymentDate
            {
                Date = NewRandomDateTime(),
                Year = _year.GetValueOrDefault(NewRandomYear()),
                Occurrence = _occurence.GetValueOrDefault(NewRandomNumberBetween(0, 3)),
                TypeValue = _typeValue ?? NewRandomMonth(),
                Type = _type.GetValueOrDefault(NewRandomEnum<ProfilePeriodType>())
            };
        }
    }
}