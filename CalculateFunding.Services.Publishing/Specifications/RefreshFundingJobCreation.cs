using CalculateFunding.Common.JobManagement;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Publishing.Interfaces;
using Serilog;

namespace CalculateFunding.Services.Publishing.Specifications
{
    public class RefreshFundingJobCreation : JobCreationForSpecification, ICreateRefreshFundingJobs
    {
        public RefreshFundingJobCreation(IJobManagement jobs, ILogger logger) 
            : base(jobs, logger, JobConstants.DefinitionNames.RefreshFundingJob, "Requesting publication of specification")
        {
        }
    }
}