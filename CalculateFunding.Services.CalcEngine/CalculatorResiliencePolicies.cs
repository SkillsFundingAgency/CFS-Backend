using CalculateFunding.Services.Calculator.Interfaces;
using CalculateFunding.Services.Core.Interfaces;
using Polly;

namespace CalculateFunding.Services.Calculator
{
    public class CalculatorResiliencePolicies : ICalculatorResiliencePolicies, IJobHelperResiliencePolicies
    {
        public Policy CacheProvider { get; set; }

        public Policy Messenger { get; set; }

        public Policy ProviderSourceDatasetsRepository { get; set; }

        public Policy ProviderResultsRepository { get; set; }

        public Policy CalculationsRepository { get; set; }

        public Policy JobsApiClient { get; set; }
    }
}
