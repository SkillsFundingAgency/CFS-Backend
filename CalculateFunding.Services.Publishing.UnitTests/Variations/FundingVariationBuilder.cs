using System.Collections.Generic;
using CalculateFunding.Common.ApiClient.Policies.Models;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Publishing.UnitTests.Variations
{
    public class FundingVariationBuilder : TestEntityBuilder
    {
        private string _name;
        private int? _order;
        private IEnumerable<string> _fundingLineCodes;

        public FundingVariationBuilder WithFundingLineCodes(params string[] fundingLineCodes)
        {
            _fundingLineCodes = fundingLineCodes;

            return this;
        }
        
        public FundingVariationBuilder WithName(string name)
        {
            _name = name;

            return this;
        }

        public FundingVariationBuilder WithOrder(int order)
        {
            _order = order;

            return this;
        }

        public FundingVariation Build()
        {
            return new FundingVariation
            {
                Name = _name ?? NewRandomString(),
                Order = _order.GetValueOrDefault(NewRandomNumberBetween(1, 100)),
                FundingLineCodes = _fundingLineCodes
            };
        }
    }
}