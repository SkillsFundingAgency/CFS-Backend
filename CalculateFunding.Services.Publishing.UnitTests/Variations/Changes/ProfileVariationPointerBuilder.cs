using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Publishing.UnitTests.Variations.Changes
{
    public class ProfileVariationPointerBuilder : TestEntityBuilder
    {
        public string _fundingLineId;
        private string _typeValue;
        private int? _year;
        private int? _occurence;
        public string _fundingStreamId;
        public string _periodType;

        public ProfileVariationPointerBuilder WithFundingLineId(string fundingLineId)
        {
            _fundingLineId = fundingLineId;

            return this;
        }

        public ProfileVariationPointerBuilder WithTypeValue(string typeValue)
        {
            _typeValue = typeValue;

            return this;
        }

        public ProfileVariationPointerBuilder WithYear(int year)
        {
            _year = year;

            return this;
        }

        public ProfileVariationPointerBuilder WithOccurence(int occurence)
        {
            _occurence = occurence;

            return this;
        }

        public ProfileVariationPointerBuilder WithFundingStreamId(string fundingStreamId)
        {
            _fundingStreamId = fundingStreamId;

            return this;
        }

        public ProfileVariationPointerBuilder WithPeriodType(string periodType)
        {
            _periodType = periodType;

            return this;
        }

        public ProfileVariationPointer Build()
        {
            return new ProfileVariationPointer
            {
                Year = _year.GetValueOrDefault(NewRandomDateTime().Year),
                Occurrence = _occurence.GetValueOrDefault(NewRandomNumberBetween(0, 4)),
                TypeValue = _typeValue ?? NewRandomMonth(),
                FundingLineId = _fundingLineId ?? NewRandomString(),
                FundingStreamId = _fundingStreamId ?? NewRandomString(),
                PeriodType = _periodType ?? NewRandomString()
            };
        }
    }
}