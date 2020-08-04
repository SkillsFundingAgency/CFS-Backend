using CalculateFunding.Common.Models;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Results.UnitTests
{
    public class FundingLineResultBuilder : TestEntityBuilder
    {
        private Reference _fundingLine;
        private decimal? _value;

        public FundingLineResultBuilder WithFundingLine(Reference fundingLine)
        {
            _fundingLine = fundingLine;

            return this;
        }

        public FundingLineResultBuilder WithValue(decimal? value)
        {
            _value = value;

            return this;
        }

        public FundingLineResult Build()
        {
            return new FundingLineResult
            {
                FundingLine = _fundingLine,
                Value = _value,
            };
        }

    }
}
