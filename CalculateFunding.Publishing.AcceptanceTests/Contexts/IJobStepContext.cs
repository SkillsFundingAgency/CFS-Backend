using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.JobManagement;

namespace CalculateFunding.Publishing.AcceptanceTests.Contexts
{
    public interface IJobStepContext
    {
        Repositories.JobsInMemoryRepository InMemoryRepo { get; }
        Common.ApiClient.Jobs.IJobsApiClient JobsClient { get; }
        JobCreateModel JobToCreate { get; set; }

        IJobManagement JobManagement { get; }
    }
}
