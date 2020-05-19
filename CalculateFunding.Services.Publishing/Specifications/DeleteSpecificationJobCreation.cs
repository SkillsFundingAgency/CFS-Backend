using CalculateFunding.Common.JobManagement;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Publishing.Interfaces;
using Serilog;

namespace CalculateFunding.Services.Publishing.Specifications
{
    public class DeleteSpecificationJobCreation : JobCreationForSpecification, ICreateDeleteSpecificationJobs
    {
        public DeleteSpecificationJobCreation(IJobManagement jobs, ILogger logger) 
            : base(jobs, logger, JobConstants.DefinitionNames.DeleteSpecificationJob, "Requesting specification deletion")
        {
        }
    }
}