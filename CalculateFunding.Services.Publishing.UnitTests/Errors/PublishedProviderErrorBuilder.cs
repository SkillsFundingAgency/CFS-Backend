using CalculateFunding.Models.Publishing;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Publishing.UnitTests.Errors
{
    public class PublishedProviderErrorBuilder : TestEntityBuilder
    {
        private PublishedProviderErrorType? _type;
        private string _fundingLineCode;
        private string _description;

        public PublishedProviderErrorBuilder WithType(PublishedProviderErrorType type)
        {
            _type = type;

            return this;
        }

        public PublishedProviderErrorBuilder WithFundingLineCode(string fundingLineCode)
        {
            _fundingLineCode = fundingLineCode;

            return this;
        }

        public PublishedProviderErrorBuilder WithDescription(string description)
        {
            _description = description;

            return this;
        }
        
        public PublishedProviderError Build()
        {
            return new PublishedProviderError
            {
                Type = _type.GetValueOrDefault(NewRandomEnum<PublishedProviderErrorType>()),
                Identifier = _fundingLineCode ?? NewRandomString(),
                Description = _description ?? NewRandomString()
            };
        }    
    }
}