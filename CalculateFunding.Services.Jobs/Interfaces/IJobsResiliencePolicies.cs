using Polly;

namespace CalculateFunding.Services.Jobs.Interfaces
{
    public interface IJobsResiliencePolicies
    {
        Policy JobDefinitionsRepository { get; set; }

        Policy CacheProviderPolicy { get; set; }

        Policy MessengerServicePolicy { get; set; }

        Policy JobRepository { get; set; }
    }
}
