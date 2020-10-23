using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Services.Results.Models;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Results.UnitTests
{
    public class ProviderWithResultsForSpecificationsBuilder : TestEntityBuilder
    {
        private ProviderInformation _providerInformation;
        private IEnumerable<SpecificationInformation> _specifications;
        
        public ProviderWithResultsForSpecificationsBuilder WithProviderInformation(ProviderInformation providerInformation)
        {
            _providerInformation = providerInformation;

            return this;
        }

        public ProviderWithResultsForSpecificationsBuilder WithSpecifications(params SpecificationInformation[] specifications)
        {
            _specifications = specifications;

            return this;
        }
        
        public ProviderWithResultsForSpecifications Build()
        {
            return new ProviderWithResultsForSpecifications
            {
                Provider = _providerInformation,
                Specifications = _specifications?.ToList()
            };
        }
    }
}