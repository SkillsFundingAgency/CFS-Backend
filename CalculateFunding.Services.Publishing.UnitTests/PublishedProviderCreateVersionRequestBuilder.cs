using CalculateFunding.Models.Publishing;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Publishing.UnitTests
{
    public class PublishedProviderCreateVersionRequestBuilder : TestEntityBuilder
    {
        private PublishedProvider _publishedProvider;
        private PublishedProviderVersion _newVersion;

        public PublishedProviderCreateVersionRequestBuilder WithPublishedProvider(PublishedProvider publishedProvider)
        {
            _publishedProvider = publishedProvider;

            return this;
        }

        public PublishedProviderCreateVersionRequestBuilder WithNewVersion(PublishedProviderVersion newVersion)
        {
            _newVersion = newVersion;

            return this;
        }
        
        public PublishedProviderCreateVersionRequest Build()
        {
            return new PublishedProviderCreateVersionRequest
            {
                NewVersion = _newVersion,
                PublishedProvider = _publishedProvider
            };
        }     
    }
}