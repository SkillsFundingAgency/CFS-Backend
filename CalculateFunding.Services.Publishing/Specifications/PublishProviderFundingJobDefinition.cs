using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Publishing.Interfaces;

namespace CalculateFunding.Services.Publishing.Specifications
{
    public class PublishProviderFundingJobDefinition : IJobDefinition
    {
        public string TriggerMessage => "Requesting publication of provider funding";
        public string Id => JobConstants.DefinitionNames.PublishProviderFundingJob;
    }
}
