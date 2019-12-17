using CalculateFunding.Common.ApiClient.Jobs;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Publishing.Interfaces;
using Serilog;

namespace CalculateFunding.Services.Publishing.Specifications
{
    public class RefreshFundingJobCreation : JobCreationForSpecification, ICreateRefreshFundingJobs
    {
        public RefreshFundingJobCreation(IJobsApiClient jobs, IPublishingResiliencePolicies resiliencePolicies, ILogger logger) 
            : base(jobs, resiliencePolicies, logger, JobConstants.DefinitionNames.RefreshFundingJob, "Requesting publication of specification")
        {
        }
    }
}