using CalculateFunding.Common.ApiClient.Providers.Models;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Datasets.Services
{
    public class ApiProviderSummaryBuilder : TestEntityBuilder
    {
        private string _id;
        private string _upin;
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
        
        public ProviderSummary Build()
        {
            return new ProviderSummary
            {
                Id = _id ?? NewRandomString(),
                UPIN = _upin ?? NewRandomString(),
                LACode = _laCode ?? NewRandomString()
            };
        }
    }
}