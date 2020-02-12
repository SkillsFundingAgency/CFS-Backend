using CalculateFunding.Common.ApiClient.Jobs;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Publishing.AcceptanceTests.Repositories;

namespace CalculateFunding.Publishing.AcceptanceTests.Contexts
{
    public class JobStepContext : IJobStepContext
    {
        public JobStepContext(IJobsApiClient jobsClient, 
            IJobManagement jobManagement)
        {
            JobsClient = jobsClient;
            JobManagement = jobManagement;
        }

        public JobsInMemoryRepository InMemoryRepo => (JobsInMemoryRepository) JobsClient;

        public IJobsApiClient JobsClient { get; set; }

        public JobCreateModel JobToCreate { get; set; }

        public IJobManagement JobManagement { get; set; }
    }
}
