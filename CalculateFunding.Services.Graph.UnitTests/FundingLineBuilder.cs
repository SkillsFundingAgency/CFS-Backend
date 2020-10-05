using CalculateFunding.Models.Graph;
using CalculateFunding.Tests.Common.Helpers;
using System.Collections.Generic;

namespace CalculateFunding.Services.Graph.UnitTests
{
    public class FundingLineBuilder : TestEntityBuilder
    {
        private string _name;
        private string _fundingLineId;

        public FundingLineBuilder WithName(string name)
        {
            _name = name;

            return this;
        }

        public FundingLineBuilder WithSpecificationId(string fundingLineId)
        {
            _fundingLineId = fundingLineId;

            return this;
        }

        public FundingLine Build()
        {
            return new FundingLine
            {
                FundingLineId = _fundingLineId ?? new RandomString(),
                FundingLineName = _name ?? new RandomString()
            };
        }
    }
}