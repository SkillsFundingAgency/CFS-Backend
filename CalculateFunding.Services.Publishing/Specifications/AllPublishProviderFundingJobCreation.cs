using CalculateFunding.Common.ApiClient.Jobs;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Publishing.Interfaces;
using Serilog;

namespace CalculateFunding.Services.Publishing.Specifications
{
    public class AllPublishProviderFundingJobCreation : JobCreationForSpecification, ICreateAllPublishProviderFundingJobs
    {
        public AllPublishProviderFundingJobCreation(IJobsApiClient jobs, IPublishingResiliencePolicies resiliencePolicies, ILogger logger) 
            : base(jobs, resiliencePolicies, logger, JobConstants.DefinitionNames.PublishAllProviderFundingJob, "Requesting publication of all provider funding")
        {
        }
    }
}