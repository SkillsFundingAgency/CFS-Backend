using CalculateFunding.Models.Providers;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Providers.UnitTests
{
    public class CurrentProviderVersionBuilder : TestEntityBuilder
    {
        private string _id;
        private string _providerVersionId;

        public CurrentProviderVersionBuilder ForFundingStreamId(string fundingStreamId)
        {
            _id = $"Current_{fundingStreamId}";

            return this;
        }

        public CurrentProviderVersionBuilder WithProviderVersionId(string providerVersionId)
        {
            _providerVersionId = providerVersionId;

            return this;
        }
        
        public CurrentProviderVersion Build()
        {
            return new CurrentProviderVersion
            {
                Id = _id ?? NewRandomString(),
                ProviderVersionId = _providerVersionId ?? NewRandomString()
            };
        }
    }
}