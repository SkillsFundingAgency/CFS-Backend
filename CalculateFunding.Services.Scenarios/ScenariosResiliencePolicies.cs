using CalculateFunding.Services.Scenarios.Interfaces;
using Polly;

namespace CalculateFunding.Services.Scenarios
{
    public class ScenariosResiliencePolicies : IScenariosResiliencePolicies
    {
        public AsyncPolicy CalcsRepository { get; set; }

        public AsyncPolicy JobsApiClient { get; set; }

        public AsyncPolicy DatasetsApiClient { get; set; }

        public AsyncPolicy ScenariosRepository { get; set; }

        public AsyncPolicy SpecificationsApiClient { get; set; }

        public AsyncPolicy ScenariosApiClient { get; set; }
    }
}
