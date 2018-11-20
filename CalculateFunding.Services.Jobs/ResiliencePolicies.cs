using CalculateFunding.Services.Jobs.Interfaces;
using Polly;

namespace CalculateFunding.Services.Jobs
{
    public class ResiliencePolicies : IJobsResilliencePolicies
    {
        public Policy JobDefinitionsRepository { get; set; }

        public Policy CacheProviderPolicy { get; set; }
    }
}
