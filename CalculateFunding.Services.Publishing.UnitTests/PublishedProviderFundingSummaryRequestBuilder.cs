using CalculateFunding.Services.Publishing.Models;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Publishing.UnitTests
{
    public class PublishedProviderFundingSummaryRequestBuilder : TestEntityBuilder
    {
        private string[] _providers;
        private string[] _channelCodes;

        public PublishedProviderFundingSummaryRequestBuilder WithProviders(string[] providers)
        {
            _providers = providers;

            return this;
        }

        public PublishedProviderFundingSummaryRequestBuilder WithChannelCodes(string[] channelCodes)
        {
            _channelCodes = channelCodes;

            return this;
        }

        public ReleaseFundingPublishProvidersRequest Build()
        {
            return new ReleaseFundingPublishProvidersRequest
            {
                PublishedProviderIds = _providers,
                ChannelCodes = _channelCodes
            };
        }
    }
}
