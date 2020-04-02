using Polly;

namespace CalculateFunding.Services.Scenarios.Interfaces
{
    public interface IScenariosResiliencePolicies
    {
        AsyncPolicy CalcsRepository { get; set; }

        AsyncPolicy JobsApiClient { get; set; }

        AsyncPolicy DatasetRepository { get; set; }

        AsyncPolicy ScenariosRepository { get; set; }

        AsyncPolicy SpecificationsApiClient { get; set; }
    }
}
