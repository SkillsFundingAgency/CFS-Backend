using CalculateFunding.Services.Jobs.Interfaces;
using Polly;

namespace CalculateFunding.Services.Jobs
{
    public class ResiliencePolicies : IJobsResiliencePolicies
    {
        public AsyncPolicy JobDefinitionsRepository { get; set; }

        public AsyncPolicy CacheProviderPolicy { get; set; }

        public AsyncPolicy MessengerServicePolicy { get; set; }

        public AsyncPolicy JobRepository { get; set; }
    }
}
