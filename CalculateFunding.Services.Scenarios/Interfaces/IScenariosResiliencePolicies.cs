using Polly;

namespace CalculateFunding.Services.Scenarios.Interfaces
{
    public interface IScenariosResiliencePolicies
    {
        Policy CalcsRepository { get; set; }

        Policy JobsApiClient { get; set; }

        Policy DatasetRepository { get; set; }

        Policy ScenariosRepository { get; set; }

        Policy SpecificationsApiClient { get; set; }
    }
}
