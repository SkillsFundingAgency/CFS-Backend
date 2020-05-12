using CalculateFunding.Common.ApiClient.Providers.Models;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Publishing.UnitTests
{
    public class ApiProviderBuilder : TestEntityBuilder
    {
        private string _providerId;
        private string _upin;
        private string _laCode;

        public ApiProviderBuilder WithLACode(string laCode)
        {
            _laCode = laCode;

            return this;
        }

        public ApiProviderBuilder WithProviderId(string providerId)
        {
            _providerId = providerId;

            return this;
        }

        public ApiProviderBuilder WithUPIN(string upin)
        {
            _upin = upin;

            return this;
        }
        
        public Provider Build()
        {
            return new Provider
            {
                ProviderId = _providerId ?? NewRandomString(),
                UPIN = _upin ?? NewRandomString(),
                LACode = _laCode ?? NewRandomString()
            };
        }
    }
}