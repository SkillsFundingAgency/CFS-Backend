using Polly;

namespace CalculateFunding.Services.Jobs.Interfaces
{
    public interface IJobsResiliencePolicies
    {
        AsyncPolicy JobDefinitionsRepository { get; set; }

        AsyncPolicy CacheProviderPolicy { get; set; }

        AsyncPolicy MessengerServicePolicy { get; set; }

        AsyncPolicy JobRepository { get; set; }
    }
}
