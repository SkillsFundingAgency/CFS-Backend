using CalculateFunding.Common.JobManagement;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Publishing.Interfaces;
using Serilog;

namespace CalculateFunding.Services.Publishing.Specifications
{
    public class ProcessDatasetObsoleteItemsJobCreation : JobCreationForSpecification, ICreateProcessDatasetObsoleteItemsJob
    {
        public ProcessDatasetObsoleteItemsJobCreation(IJobManagement jobs, ILogger logger) 
            : base(jobs, 
                logger, 
                JobConstants.DefinitionNames.ProcessDatasetObsoleteItems, 
                "Process dataset obsolete items by publishing change")
        {
        }
    }
}