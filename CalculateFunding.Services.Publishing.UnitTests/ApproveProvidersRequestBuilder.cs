using CalculateFunding.Services.Publishing.Models;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Publishing.UnitTests
{
    public class ApproveProvidersRequestBuilder : TestEntityBuilder
    {
        private string[] _providers;

        public ApproveProvidersRequestBuilder WithProviders(string[] providers)
        {
            _providers = providers;

            return this;
        }

        public ApproveProvidersRequest Build()
        {
            return new ApproveProvidersRequest
            {
                Providers = _providers
            };
        }
    }
}
