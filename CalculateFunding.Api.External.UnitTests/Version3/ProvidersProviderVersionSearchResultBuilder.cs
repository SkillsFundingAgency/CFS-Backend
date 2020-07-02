using CalculateFunding.Common.ApiClient.Providers.Models.Search;

namespace CalculateFunding.Api.External.UnitTests.Version3
{
    public class ProvidersProviderVersionSearchResultBuilder
    {
        private string _providerId;

        public ProvidersProviderVersionSearchResultBuilder WithProviderId(string providerId)
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
