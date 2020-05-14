using CalculateFunding.Services.Publishing.Undo;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Publishing.UnitTests.Undo
{
    public class PublishedFundingUndoTaskContextBuilder : TestEntityBuilder
    {
        private PublishedFundingUndoJobParameters _parameters;
        private CorrelationIdDetails _publishedFundingDetails;
        private CorrelationIdDetails _publishedFundingVersionDetails;
        private CorrelationIdDetails _publishedProviderDetails;
        private CorrelationIdDetails _publishedProviderVersionDetails;

        public PublishedFundingUndoTaskContextBuilder WithParameters(PublishedFundingUndoJobParameters parameters)
        {
            _parameters = parameters;

            return this;
        }

        public PublishedFundingUndoTaskContextBuilder WithPublishedFundingDetails(CorrelationIdDetails details)
        {
            _publishedFundingDetails = details;

            return this;
        }
        
        public PublishedFundingUndoTaskContextBuilder WithPublishedFundingVersionDetails(CorrelationIdDetails details)
        {
            _publishedFundingVersionDetails = details;

            return this;
        }
        
        public PublishedFundingUndoTaskContextBuilder WithPublishedProviderVersionDetails(CorrelationIdDetails details)
        {
            _publishedProviderVersionDetails = details;

            return this;
        }
        
        public PublishedFundingUndoTaskContextBuilder WithPublishedProviderDetails(CorrelationIdDetails details)
        {
            _publishedProviderDetails = details;

            return this;
        }

        public PublishedFundingUndoTaskContext Build()
        {
            return new PublishedFundingUndoTaskContext(
                _parameters
                ?? new PublishedFundingUndoJobParametersBuilder().Build())
            {
                PublishedFundingDetails = _publishedFundingDetails,
                PublishedFundingVersionDetails = _publishedFundingVersionDetails,
                PublishedProviderDetails = _publishedProviderDetails,
                PublishedProviderVersionDetails = _publishedProviderVersionDetails
            };
        }
    }
}