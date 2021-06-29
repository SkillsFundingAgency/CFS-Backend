using CalculateFunding.Services.Publishing.Undo;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Publishing.UnitTests.Undo
{
    public class PublishedFundingUndoTaskContextBuilder : TestEntityBuilder
    {
        private PublishedFundingUndoJobParameters _parameters;
        private UndoTaskDetails _publishedProviderVersionDetails;

        public PublishedFundingUndoTaskContextBuilder WithParameters(PublishedFundingUndoJobParameters parameters)
        {
            _parameters = parameters;

            return this;
        }
        
        public PublishedFundingUndoTaskContextBuilder WithPublishedProviderVersionDetails(UndoTaskDetails details)
        {
            _publishedProviderVersionDetails = details;

            return this;
        }

        public PublishedFundingUndoTaskContext Build()
        {
            return new PublishedFundingUndoTaskContext(
                _parameters
                ?? new PublishedFundingUndoJobParametersBuilder().Build())
            {
                UndoTaskDetails = _publishedProviderVersionDetails
            };
        }
    }
}