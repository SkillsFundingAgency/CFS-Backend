using CalculateFunding.Common.ApiClient.Calcs;
using CalculateFunding.Common.ApiClient.Providers;
using CalculateFunding.Publishing.AcceptanceTests.Repositories;
using CalculateFunding.Services.Publishing.Interfaces;

namespace CalculateFunding.Publishing.AcceptanceTests.Contexts
{
    public interface ICalculationsStepContext
    {
        ICalculationsApiClient Client { get; set; }

        CalculationsInMemoryClient EmulatedClient { get; set; }
    }
}
