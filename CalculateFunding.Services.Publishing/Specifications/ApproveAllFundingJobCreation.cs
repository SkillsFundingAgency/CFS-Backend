using CalculateFunding.Common.JobManagement;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Publishing.Interfaces;
using Serilog;

namespace CalculateFunding.Services.Publishing.Specifications
{
    public class ApproveAllFundingJobCreation : JobCreationForSpecification, ICreateApproveAllFundingJobs
    {
        public ApproveAllFundingJobCreation(IJobManagement jobs, ILogger logger) 
            : base(jobs, logger, JobConstants.DefinitionNames.
                  ApproveAllProviderFundingJob, "Requesting all funding approval")
        {
        }
    }
}