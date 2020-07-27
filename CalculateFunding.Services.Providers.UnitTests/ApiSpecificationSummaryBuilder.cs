using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Providers.UnitTests
{
    public class ApiSpecificationSummaryBuilder : TestEntityBuilder
    {
        private string _id;
        private string _providerVersionId;
        private ProviderSource? _providerSource;

        public ApiSpecificationSummaryBuilder WithId(string id)
        {
            _id = id;

            return this;
        }

        public ApiSpecificationSummaryBuilder WithProviderVersionId(string providerVersionId)
        {
            _providerVersionId = providerVersionId;

            return this;
        }

        public ApiSpecificationSummaryBuilder WithProviderSource(ProviderSource providerSource)
        {
            _providerSource = providerSource;

            return this;
        }
        
        public SpecificationSummary Build()
        {
            return new SpecificationSummary
            {
                Id = _id ?? NewRandomString(),
                ProviderVersionId = _providerVersionId ?? NewRandomString(),
                ProviderSource = _providerSource.GetValueOrDefault(NewRandomEnum<ProviderSource>())
            };
        }
    }
}