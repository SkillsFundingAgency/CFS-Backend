using CalculateFunding.Common.ApiClient.Policies.Models;
using CalculateFunding.Tests.Common.Helpers;
using System;
using System.Collections.Generic;

namespace CalculateFunding.Api.Policy.IntegrationTests.Data
{
    public class FundingVariationBuilder : TestEntityBuilder
    {
        private string _name;
        private int _order;
        private IEnumerable<string> _fundingLineCodes;

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

        public FundingVariationBuilder WithFundingLineCodes(params string[] fundingLineCodes)
        {
            _fundingLineCodes = fundingLineCodes;
            return this;
        }

        public FundingVariation Build()
            => new FundingVariation
            {
                Name = _name ?? NewRandomString(),
                Order = _order,
                FundingLineCodes = _fundingLineCodes ?? Array.Empty<string>()
            };
    }
}
