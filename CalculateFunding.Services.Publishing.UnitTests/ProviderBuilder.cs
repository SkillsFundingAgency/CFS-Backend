using CalculateFunding.Models.Publishing;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Publishing.UnitTests
{
    public class ProviderBuilder : TestEntityBuilder
    {
        private string _providerId;

        public ProviderBuilder WithProviderId(string providerId)
        {
            _providerId = providerId;

            return this;
        }

        public Provider Build()
        {
            return new Provider
            {
                ProviderId = _providerId ?? NewRandomString()
            };
        }
    }
}