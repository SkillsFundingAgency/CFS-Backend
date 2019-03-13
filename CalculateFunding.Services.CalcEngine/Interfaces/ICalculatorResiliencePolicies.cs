using Polly;

namespace CalculateFunding.Services.Calculator.Interfaces
{
    public interface ICalculatorResiliencePolicies
    {
        Policy CacheProvider { get; set; }

        Policy Messenger { get; set; }

        Policy ProviderSourceDatasetsRepository { get; set; }

        Policy ProviderResultsRepository { get; set; }

        Policy CalculationsRepository { get; set; }

        Policy JobsApiClient { get; set; }
    }
}
