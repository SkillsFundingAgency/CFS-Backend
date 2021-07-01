using System;
using CalculateFunding.Common.ApiClient.Publishing.Models;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Api.Publishing.IntegrationTests.RefreshFunding
{
    public class FundingStreamPaymentDateBuilder : TestEntityBuilder
    {
        private string _typeValue;
        private ProfilePeriodType? _type;
        private int? _year;
        private int? _occurence;
        private DateTimeOffset? _date;

        public FundingStreamPaymentDateBuilder WithDate(string dateLiteral)
        {
            _date = DateTime.Parse(dateLiteral);

            return this;
        }
        
        public FundingStreamPaymentDateBuilder WithDate(DateTime date)
        {
            _date = date;

            return this;
        }
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
                Date = _date.GetValueOrDefault(NewRandomDateTime()),
                Year = _year.GetValueOrDefault(NewRandomYear()),
                Occurrence = _occurence.GetValueOrDefault(NewRandomNumberBetween(0, 3)),
                TypeValue = _typeValue ?? NewRandomMonth(),
                Type = _type.GetValueOrDefault(NewRandomEnum<ProfilePeriodType>())
            };
        }
    }
}