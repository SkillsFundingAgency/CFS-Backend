using CalculateFunding.Models.Publishing;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Publishing.UnitTests.Errors
{
    public class PublishedProviderErrorBuilder : TestEntityBuilder
    {
        private PublishedProviderErrorType? _type;
        private string _identifier;
        private string _summaryErrorMessage;
        private string _detailedErrorMessage;
        private string _fundingStreamId;
        private string _fundingLineCode;
        private bool _withoutFundingLineCode;

        public PublishedProviderErrorBuilder WithoutFundingLineCode()
        {
            _withoutFundingLineCode = true;

            return this;
        }
        

        public PublishedProviderErrorBuilder WithType(PublishedProviderErrorType type)
        {
            _type = type;

            return this;
        }

        public PublishedProviderErrorBuilder WithIdentifier(string identifier)
        {
            _identifier = identifier;

            return this;
        }

        public PublishedProviderErrorBuilder WithFundingStreamId(string fundingStreamId)
        {
            _fundingStreamId = fundingStreamId;

            return this;
        }

        public PublishedProviderErrorBuilder WithSummaryErrorMessage(string summaryErrorMessage)
        {
            _summaryErrorMessage = summaryErrorMessage;

            return this;
        }

        public PublishedProviderErrorBuilder WithDetailedErrorMessage(string detailedErrorMessage)
        {
            _detailedErrorMessage = detailedErrorMessage;

            return this;
        }

        public PublishedProviderErrorBuilder WithFundingLine(string fundingLineCode)
        {
            _fundingLineCode = fundingLineCode;

            return this;
        }

        public PublishedProviderError Build()
        {
            return new PublishedProviderError
            {
                Type = _type.GetValueOrDefault(NewRandomEnum<PublishedProviderErrorType>()),
                Identifier = _identifier ?? NewRandomString(),
                SummaryErrorMessage = _summaryErrorMessage ?? NewRandomString(),
                DetailedErrorMessage = _detailedErrorMessage ?? NewRandomString(),
                FundingStreamId = _fundingStreamId ?? NewRandomString(),
                FundingLineCode = _withoutFundingLineCode ? null : _fundingLineCode ?? NewRandomString()
            };
        }    
    }
}