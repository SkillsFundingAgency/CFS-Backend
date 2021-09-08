using CalculateFunding.Common.ApiClient.Publishing.Models;
using CalculateFunding.Tests.Common.Helpers;
using System;

namespace CalculateFunding.Api.Publishing.IntegrationTests.ReleaseManagement
{
    public class ReleaseProvidersToChannelRequestBuilder : TestEntityBuilder
    {
        private string[] _channels;
        private string[] _providerIds;

        public ReleaseProvidersToChannelRequestBuilder WithProviderIds(params string[] providerIds)
        {
            _providerIds = providerIds;

            return this;
        }

        public ReleaseProvidersToChannelRequestBuilder WithChannels(params string[] channels)
        {
            _channels = channels;

            return this;
        }

        public ReleaseProvidersToChannelRequest Build() =>
            new ReleaseProvidersToChannelRequest
            {
                Channels = _channels ?? Array.Empty<string>(),
                ProviderIds = _providerIds ?? Array.Empty<string>()
            };

    }
}
