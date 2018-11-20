using Polly;

namespace CalculateFunding.Services.Jobs.Interfaces
{
    public interface IJobsResilliencePolicies
    {
        Policy JobDefinitionsRepository { get; set; }

        Policy CacheProviderPolicy { get; set; }
    }
}
