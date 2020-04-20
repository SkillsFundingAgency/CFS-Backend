using CalculateFunding.Common.ApiClient.Calcs.Models;
using CalculateFunding.Models.Publishing;
using TemplateFundingLine = CalculateFunding.Common.TemplateMetadata.Models.FundingLine;

namespace CalculateFunding.Services.Publishing
{
    public interface IPublishProviderExclusionCheck
    {
        PublishedProviderExclusionCheckResult ShouldBeExcluded(ProviderCalculationResult providerCalculationResult, TemplateMapping templateMapping, Common.TemplateMetadata.Models.Calculation[] flattedCalculations);
    }
}