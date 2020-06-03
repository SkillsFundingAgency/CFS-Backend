using CalculateFunding.Models.Providers.Requests;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Providers.UnitTests.Validation
{
    public class SetFundingStreamCurrentProviderVersionRequestBuilder : TestEntityBuilder
    {
        private string _fundingStreamId;
        private string _providerVersionId;

        public SetFundingStreamCurrentProviderVersionRequestBuilder WithFundingStreamId(string fundingStreamId)
        {
            _fundingStreamId = fundingStreamId;

            return this;
        }

        public SetFundingStreamCurrentProviderVersionRequestBuilder WithProviderVersionId(string providerVersionId)
        {
            _providerVersionId = providerVersionId;

            return this;
        }

        public SetFundingStreamCurrentProviderVersionRequest Build() =>
            new SetFundingStreamCurrentProviderVersionRequest
            {
                FundingStreamId = _fundingStreamId,
                ProviderVersionId = _providerVersionId
            };
    }
}