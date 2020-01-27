using CalculateFunding.Services.CalcEngine.Interfaces;
using CalculateFunding.Services.DeadletterProcessor;
using Polly;

namespace CalculateFunding.Services.CalcEngine
{
    public class CalculatorResiliencePolicies : ICalculatorResiliencePolicies, IJobHelperResiliencePolicies
    {
        public Policy CacheProvider { get; set; }

        public Policy Messenger { get; set; }

        public Policy ProviderSourceDatasetsRepository { get; set; }

        public Policy ProviderResultsRepository { get; set; }

        public Policy CalculationsRepository { get; set; }

        public Policy JobsApiClient { get; set; }

        public Policy SpecificationsApiClient { get; set; }
    }
}
