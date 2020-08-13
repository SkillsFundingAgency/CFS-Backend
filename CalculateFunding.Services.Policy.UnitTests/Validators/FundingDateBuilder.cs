using CalculateFunding.Models.Policy.FundingPolicy;
using CalculateFunding.Tests.Common.Helpers;
using System.Collections.Generic;

namespace CalculateFunding.Services.Policy.Validators
{
    public class FundingDateBuilder : TestEntityBuilder
    {
        private string _fundingStreamId;
        private string _fundingPeriodId;
        private IEnumerable<FundingDatePattern> _patterns;

        public FundingDateBuilder WithFundingStreamId(string fundingStreamId)
        {
            _fundingStreamId = fundingStreamId;

            return this;
        }

        public FundingDateBuilder WithFundingPeriodId(string fundingPeriodId)
        {
            _fundingPeriodId = fundingPeriodId;

            return this;
        }

        public FundingDateBuilder WithPatterns(IEnumerable<FundingDatePattern> patterns)
        {
            _patterns = patterns;

            return this;
        }

        public FundingDate Build()
        {
            return new FundingDate
            {
                FundingPeriodId = _fundingPeriodId ?? NewRandomString(),
                FundingStreamId = _fundingStreamId ?? NewRandomString(),
                Patterns = _patterns
            };
        }
    }
}
