using CalculateFunding.Common.ApiClient.Jobs;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Publishing.Interfaces;
using Serilog;

namespace CalculateFunding.Services.Publishing.Specifications
{
    public class ApproveAllFundingJobCreation : JobCreationForSpecification, ICreateApproveAllFundingJobs
    {
        public ApproveAllFundingJobCreation(IJobsApiClient jobs, IPublishingResiliencePolicies resiliencePolicies, ILogger logger) 
            : base(jobs, resiliencePolicies, logger, JobConstants.DefinitionNames.
                  ApproveAllProviderFundingJob, "Requesting all funding approval")
        {
        }
    }
}