using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.JobManagement;

namespace CalculateFunding.Publishing.AcceptanceTests.Contexts
{
    public interface IJobStepContext
    {
        Repositories.JobsInMemoryRepository InMemoryRepo { get; set; }
        Common.ApiClient.Jobs.IJobsApiClient JobsClient { get; set; }
        JobCreateModel JobToCreate { get; set; }
        IJobManagement JobManagement { get; set; }
    }
}
