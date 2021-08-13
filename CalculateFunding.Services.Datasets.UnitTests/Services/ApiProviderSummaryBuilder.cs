using CalculateFunding.Common.ApiClient.Providers.Models;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Datasets.Services
{
    public class ApiProviderSummaryBuilder : TestEntityBuilder
    {
        private string _id;
        private string _upin;
        private string _ukprn;
        private string _laCode;

        public ApiProviderSummaryBuilder WithLACode(string laCode)
        {
            _laCode = laCode;

            return this;
        }

        public ApiProviderSummaryBuilder WithId(string id)
        {
            _id = id;

            return this;
        }

        public ApiProviderSummaryBuilder WithUPIN(string upin)
        {
            _upin = upin;

            return this;
        }

        public ApiProviderSummaryBuilder WithUKPRN(string ukprn)
        {
            _ukprn = ukprn;

            return this;
        }

        public ProviderSummary Build()
        {
            return new ProviderSummary
            {
                Id = _id ?? NewRandomString(),
                UKPRN = _ukprn ?? NewRandomString(),
                UPIN = _upin ?? NewRandomString(),
                LACode = _laCode ?? NewRandomString()
            };
        }
    }
}