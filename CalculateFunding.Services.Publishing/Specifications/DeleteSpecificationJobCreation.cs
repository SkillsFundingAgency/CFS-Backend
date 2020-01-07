using CalculateFunding.Common.ApiClient.Jobs;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Publishing.Interfaces;
using Serilog;

namespace CalculateFunding.Services.Publishing.Specifications
{
    public class DeleteSpecificationJobCreation : JobCreationForSpecification, ICreateDeleteSpecificationJobs
    {
        public DeleteSpecificationJobCreation(IJobsApiClient jobs, IPublishingResiliencePolicies resiliencePolicies, ILogger logger) 
            : base(jobs, resiliencePolicies, logger, JobConstants.DefinitionNames.DeleteSpecificationJob, "Requesting specification deletion")
        {
        }
    }
}