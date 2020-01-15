using CalculateFunding.Common.ApiClient.Policies.Models.FundingConfig;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Publishing.UnitTests.Variations
{
    public class FundingVariationBuilder : TestEntityBuilder
    {
        private string _name;
        private int? _order;

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
                Order = _order.GetValueOrDefault(NewRandomNumberBetween(1, 100))
            };
        }
    }
}