using CalculateFunding.Common.ApiClient.Jobs;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Publishing.Interfaces;
using Serilog;

namespace CalculateFunding.Services.Publishing.Specifications
{
    public class BatchPublishProviderFundingJobCreation : JobCreationForSpecification, ICreateBatchPublishProviderFundingJobs
    {
        public BatchPublishProviderFundingJobCreation(
            IJobsApiClient jobs, 
            IPublishingResiliencePolicies resiliencePolicies, 
            ILogger logger) 
            : base(
                  jobs, 
                  resiliencePolicies, 
                  logger, 
                  JobConstants.DefinitionNames.PublishBatchProviderFundingJob, 
                  "Requesting publication of batch provider funding")
        {
        }
    }
}