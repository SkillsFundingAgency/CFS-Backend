using Polly;

namespace CalculateFunding.Services.Scenarios.Interfaces
{
    public interface IScenariosResiliencePolicies
    {
        Policy CalcsRepository { get; set; }

        Policy JobsApiClient { get; set; }
    }
}
