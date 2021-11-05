using CalculateFunding.Api.External.V3.Models;

namespace CalculateFunding.Api.External.UnitTests.Version4
{
    public class ProviderVersionSearchResultBuilder
    {
        private string _providerId;

        public ProviderVersionSearchResultBuilder WithProviderId(string providerId)
        {
            _providerId = providerId;
            return this;
        }

        public ProviderVersionSearchResult Build()
        {
            return new ProviderVersionSearchResult
            {
                ProviderId = _providerId
            };
        }
    }
}
