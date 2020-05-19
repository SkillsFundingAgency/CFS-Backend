using CalculateFunding.Common.ApiClient.Jobs;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Publishing.Interfaces;
using Serilog;

namespace CalculateFunding.Services.Publishing.Specifications
{
    public class ApproveBatchFundingJobCreation : JobCreationForSpecification, ICreateApproveBatchFundingJobs
    {
        public ApproveBatchFundingJobCreation(
            IJobManagement jobs, 
            ILogger logger) : 
            base(
                jobs, 
                logger, 
                JobConstants.DefinitionNames.ApproveBatchProviderFundingJob,
                "Requesting batch funding approval")
        {
        }
    }
}
