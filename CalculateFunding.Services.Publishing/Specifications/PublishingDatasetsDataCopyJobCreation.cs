using CalculateFunding.Common.JobManagement;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Specifications;
using Serilog;

namespace CalculateFunding.Services.Publishing.Specifications
{
    public class PublishingDatasetsDataCopyJobCreation : JobCreationForSpecification, ICreatePublishDatasetsDataCopyJob
    {
        public PublishingDatasetsDataCopyJobCreation(IJobManagement jobs, ILogger logger) 
            : base(jobs, 
                logger, 
                JobConstants.DefinitionNames.PublishDatasetsDataJob, 
                "New Csv file generation triggered by publishing change")
        {
        }
    }
}