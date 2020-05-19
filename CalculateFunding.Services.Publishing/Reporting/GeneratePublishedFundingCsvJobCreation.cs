using CalculateFunding.Common.JobManagement;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Specifications;
using Serilog;

namespace CalculateFunding.Services.Publishing.Reporting
{
    public class GeneratePublishedFundingCsvJobCreation : JobCreationForSpecification, ICreateGeneratePublishedFundingCsvJobs
    {
        public GeneratePublishedFundingCsvJobCreation(IJobManagement jobs, ILogger logger) 
            : base(jobs, 
                logger, 
                JobConstants.DefinitionNames.GeneratePublishedFundingCsvJob, 
                "New Csv file generation triggered by publishing change")
        {
        }
    }
}