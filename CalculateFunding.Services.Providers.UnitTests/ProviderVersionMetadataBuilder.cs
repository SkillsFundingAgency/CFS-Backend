using CalculateFunding.Models.Providers;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Providers.UnitTests
{
    public class ProviderVersionMetadataBuilder : TestEntityBuilder
    {
        private string _fundingStream;

        public ProviderVersionMetadataBuilder WithFundingStream(string fundingStream)
        {
            _fundingStream = fundingStream;

            return this;
        }
        
        public ProviderVersionMetadata Build()
        {
            return new ProviderVersionMetadata
            {
                FundingStream = _fundingStream ?? NewRandomString()
            };
        }
    }
}