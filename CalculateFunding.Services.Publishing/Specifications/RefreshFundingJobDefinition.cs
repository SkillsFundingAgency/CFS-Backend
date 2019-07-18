using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Publishing.Interfaces;

namespace CalculateFunding.Services.Publishing.Specifications
{
    public class RefreshFundingJobDefinition : IJobDefinition
    {
        public string TriggerMessage => "Requesting publication of specification";
        public string Id => JobConstants.DefinitionNames.RefreshFundingJob;
    }
}
