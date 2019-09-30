using CalculateFunding.Common.ApiClient.Providers;
using CalculateFunding.Publishing.AcceptanceTests.Repositories;
using CalculateFunding.Services.Publishing.Interfaces;

namespace CalculateFunding.Publishing.AcceptanceTests.Contexts
{
    public interface IProvidersStepContext
    {
        IProviderService Service { get; set; }

        IProvidersApiClient Client { get; set; }

        ProvidersInMemoryClient EmulatedClient { get; set; }
    }
}
