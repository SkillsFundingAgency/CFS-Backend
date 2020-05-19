using CalculateFunding.Common.ApiClient.Jobs;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Publishing.Interfaces;
using Serilog;

namespace CalculateFunding.Services.Publishing.Specifications
{
    public class BatchPublishProviderFundingJobCreation : JobCreationForSpecification, ICreateBatchPublishProviderFundingJobs
    {
        public BatchPublishProviderFundingJobCreation(
            IJobManagement jobs, 
            ILogger logger) 
            : base(
                  jobs, 
                  logger, 
                  JobConstants.DefinitionNames.PublishBatchProviderFundingJob, 
                  "Requesting publication of batch provider funding")
        {
        }
    }
}