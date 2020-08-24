using CalculateFunding.Common.JobManagement;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Publishing.Interfaces;
using Serilog;

namespace CalculateFunding.Services.Publishing.Specifications
{
    public class PublishIntegrityCheckJobCreation : JobCreationForSpecification, ICreatePublishIntegrityJob
    {
        public PublishIntegrityCheckJobCreation(IJobManagement jobs, ILogger logger) 
            : base(jobs, logger, JobConstants.DefinitionNames.
                  PublishIntegrityCheckJob, "Requesting publish integrity check")
        {
        }
    }
}