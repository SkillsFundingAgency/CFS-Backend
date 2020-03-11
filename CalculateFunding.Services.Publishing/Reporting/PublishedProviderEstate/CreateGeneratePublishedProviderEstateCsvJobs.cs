using CalculateFunding.Common.ApiClient.Jobs;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Specifications;
using Serilog;

namespace CalculateFunding.Services.Publishing.Reporting.PublishedProviderEstate
{
    public class CreateGeneratePublishedProviderEstateCsvJobs : JobCreationForSpecification, ICreateGeneratePublishedProviderEstateCsvJobs
    {
        public CreateGeneratePublishedProviderEstateCsvJobs(IJobsApiClient jobs, IPublishingResiliencePolicies resiliencePolicies, ILogger logger)
            : base(jobs,
                resiliencePolicies,
                logger,
                JobConstants.DefinitionNames.GeneratePublishedProviderEstateCsvJob,
                "New Csv file generation triggered by publishing change")
        {
        }
    }
}
