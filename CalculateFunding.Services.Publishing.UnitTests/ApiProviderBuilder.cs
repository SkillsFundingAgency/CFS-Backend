using CalculateFunding.Common.ApiClient.Providers.Models;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Publishing.UnitTests
{
    public class ApiProviderBuilder : TestEntityBuilder
    {
        private string _providerId;
        private string _status;

        public ApiProviderBuilder WithProviderId(string providerId)
        {
            _providerId = providerId;

            return this;
        }

        public ApiProviderBuilder WithStatus(string status)
        {
            _status = status;

            return this;
        }
        
        public Provider Build()
        {
            return new Provider
            {
                ProviderId = _providerId ?? NewRandomString(),
                Status = _status ?? NewRandomString()
            };
        }
    }
}