using CalculateFunding.Common.JobManagement;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Publishing.Interfaces;
using Serilog;

namespace CalculateFunding.Services.Publishing.Specifications
{
    public class AllPublishProviderFundingJobCreation : JobCreationForSpecification, ICreateAllPublishProviderFundingJobs
    {
        public AllPublishProviderFundingJobCreation(IJobManagement jobs, ILogger logger) 
            : base(jobs, logger, JobConstants.DefinitionNames.PublishAllProviderFundingJob, "Requesting publication of all provider funding")
        {
        }
    }
}