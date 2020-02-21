using System.Collections.Generic;
using CalculateFunding.Common.ApiClient.Policies.Models;
using CalculateFunding.Common.ApiClient.Policies.Models.FundingConfig;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Publishing.UnitTests.Variations
{
    public class ApiFundingConfigurationBuilder : TestEntityBuilder
    {
        private IEnumerable<FundingVariation> _variations;

        public ApiFundingConfigurationBuilder WithVariations(params FundingVariation[] variations)
        {
            _variations = variations;

            return this;
        }
        
        public FundingConfiguration Build()
        {
            return new FundingConfiguration();
        }
    }
}