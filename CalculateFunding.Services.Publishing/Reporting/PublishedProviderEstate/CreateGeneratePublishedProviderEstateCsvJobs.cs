using CalculateFunding.Common.JobManagement;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Specifications;
using Serilog;

namespace CalculateFunding.Services.Publishing.Reporting.PublishedProviderEstate
{
    public class CreateGeneratePublishedProviderEstateCsvJobs : JobCreationForSpecification, ICreateGeneratePublishedProviderEstateCsvJobs
    {
        public CreateGeneratePublishedProviderEstateCsvJobs(IJobManagement jobs, ILogger logger)
            : base(jobs,
                logger,
                JobConstants.DefinitionNames.GeneratePublishedProviderEstateCsvJob,
                "New Csv file generation triggered by publishing change")
        {
        }
    }
}
