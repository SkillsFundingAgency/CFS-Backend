using CalculateFunding.Common.ApiClient.Providers.Models;
using CalculateFunding.Tests.Common.Helpers;
using System.Collections.Generic;

namespace CalculateFunding.Services.Datasets.Services
{
    public class ApiProviderVersionBuilder : TestEntityBuilder
    {
        private IEnumerable<Provider> _providers;

        public ApiProviderVersionBuilder WithProviders(IEnumerable<Provider> providers)
        {
            _providers = providers;

            return this;
        }
        
        public ProviderVersion Build()
        {
            return new ProviderVersion
            {
                Providers = _providers ?? new Provider[] { }
            };
        }
    }
}