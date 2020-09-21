using System.Collections.Generic;
using CalculateFunding.Services.Results.Models;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Results.UnitTests
{
    public class MergeSpecificationInformationRequestBuilder : TestEntityBuilder
    {
        private SpecificationInformation _specificationInformation;
        private IEnumerable<string> _providerIds;

        public MergeSpecificationInformationRequestBuilder WithSpecificationInformation(SpecificationInformation specificationInformation)
        {
            _specificationInformation = specificationInformation;

            return this;
        }

        public MergeSpecificationInformationRequestBuilder WithProviderIds(params string[] providerIds)
        {
            _providerIds = providerIds;

            return this;
        }
        
        public MergeSpecificationInformationRequest Build()
        {
            return new MergeSpecificationInformationRequest
            {
                SpecificationInformation    = _specificationInformation,
                ProviderIds = _providerIds
            };
        }
    }
}