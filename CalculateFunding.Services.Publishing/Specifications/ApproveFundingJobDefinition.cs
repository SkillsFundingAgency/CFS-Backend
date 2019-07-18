using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Publishing.Interfaces;

namespace CalculateFunding.Services.Publishing.Specifications
{
    public class ApproveFundingJobDefinition : IJobDefinition
    {
        public string TriggerMessage => "Requesting funding approval";
        public string Id => JobConstants.DefinitionNames.ApproveFunding;
    }
}
