using CalculateFunding.Common.ApiClient.Publishing.Models;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Api.Publishing.IntegrationTests.PublishedProvider
{
    public class PublishedProviderIdsRequestBuilder : TestEntityBuilder
    {
        private string[] _providers;

        public PublishedProviderIdsRequestBuilder WithProviders(string[] providers)
        {
            _providers = providers;

            return this;
        }

        public PublishedProviderIdsRequest Build()
        {
            return new PublishedProviderIdsRequest
            {
                PublishedProviderIds = _providers
            };
        }
    }
}
