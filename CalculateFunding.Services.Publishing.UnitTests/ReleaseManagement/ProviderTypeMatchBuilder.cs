using CalculateFunding.Common.ApiClient.Policies.Models;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Publishing.UnitTests.ReleaseManagement
{
    public class ProviderTypeMatchBuilder : TestEntityBuilder
    {
        private string _providerSubtype;
        private string _providerType;

        public ProviderTypeMatchBuilder WithProviderSubtype(string providerSubtype)
        {
            _providerSubtype = providerSubtype;
            return this;
        }
        public ProviderTypeMatchBuilder WithProviderType(string providerType)
        {
            _providerType = providerType;
            return this;
        }

        public ProviderTypeMatch Build()
            => new ProviderTypeMatch()
            {
                ProviderSubtype = _providerSubtype ?? NewRandomString(),
                ProviderType = _providerType ?? NewRandomString(),
            };
    }
}
