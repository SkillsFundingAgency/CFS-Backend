using CalculateFunding.Services.Jobs.Interfaces;
using Polly;

namespace CalculateFunding.Services.Jobs
{
    public class ResiliencePolicies : IJobsResiliencePolicies
    {
        public Policy JobDefinitionsRepository { get; set; }

        public Policy CacheProviderPolicy { get; set; }

        public Policy MessengerServicePolicy { get; set; }

        public Policy JobRepository { get; set; }
    }
}
