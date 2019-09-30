using CalculateFunding.Common.ApiClient.Jobs;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Publishing.AcceptanceTests.Repositories;

namespace CalculateFunding.Publishing.AcceptanceTests.Contexts
{
    public class JobStepContext : IJobStepContext
    {
        public JobsInMemoryRepository InMemoryRepo { get; set; }

        public IJobsApiClient JobsClient { get; set; }

        public JobCreateModel JobToCreate { get; set; }
    }
}
