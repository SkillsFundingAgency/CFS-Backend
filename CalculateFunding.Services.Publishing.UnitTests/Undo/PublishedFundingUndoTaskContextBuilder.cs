using CalculateFunding.Services.Publishing.Undo;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Publishing.UnitTests.Undo
{
    public class PublishedFundingUndoTaskContextBuilder : TestEntityBuilder
    {
        private PublishedFundingUndoJobParameters _parameters;
        private UndoTaskDetails _publishedFundingDetails;
        private UndoTaskDetails _publishedFundingVersionDetails;
        private UndoTaskDetails _publishedProviderDetails;
        private UndoTaskDetails _publishedProviderVersionDetails;

        public PublishedFundingUndoTaskContextBuilder WithParameters(PublishedFundingUndoJobParameters parameters)
        {
            _parameters = parameters;

            return this;
        }

        public PublishedFundingUndoTaskContextBuilder WithPublishedFundingDetails(UndoTaskDetails details)
        {
            _publishedFundingDetails = details;

            return this;
        }
        
        public PublishedFundingUndoTaskContextBuilder WithPublishedFundingVersionDetails(UndoTaskDetails details)
        {
            _publishedFundingVersionDetails = details;

            return this;
        }
        
        public PublishedFundingUndoTaskContextBuilder WithPublishedProviderVersionDetails(UndoTaskDetails details)
        {
            _publishedProviderVersionDetails = details;

            return this;
        }
        
        public PublishedFundingUndoTaskContextBuilder WithPublishedProviderDetails(UndoTaskDetails details)
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