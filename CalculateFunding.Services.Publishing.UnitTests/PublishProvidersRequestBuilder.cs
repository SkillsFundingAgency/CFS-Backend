using CalculateFunding.Services.Publishing.Models;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Publishing.UnitTests
{
    public class PublishProvidersRequestBuilder : TestEntityBuilder
    {
        private string[] _providers;

        public PublishProvidersRequestBuilder WithProviders(string[] providers)
        {
            _providers = providers;

            return this;
        }

        public PublishProvidersRequest Build()
        {
            return new PublishProvidersRequest
            {
                Providers = _providers
            };
        }
    }
}
