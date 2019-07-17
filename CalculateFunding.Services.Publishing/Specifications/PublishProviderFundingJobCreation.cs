using CalculateFunding.Common.ApiClient.Jobs;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Publishing.Interfaces;
using Serilog;

namespace CalculateFunding.Services.Publishing.Specifications
{
    public class PublishProviderFundingJobCreation : JobCreationForSpecification, ICreatePublishFundingJobs
    {
        public PublishProviderFundingJobCreation(IJobsApiClient jobs,
            IPublishingResiliencePolicies resiliencePolicies,
            ILogger logger) : base(jobs, resiliencePolicies, logger, 
            "Requesting publication of provider funding", 
            JobConstants.DefinitionNames.PublishProviderFundingJob)
        {
        }
    }
}