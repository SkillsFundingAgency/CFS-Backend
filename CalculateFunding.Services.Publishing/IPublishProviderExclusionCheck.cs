using CalculateFunding.Models.Publishing;
using TemplateFundingLine = CalculateFunding.Common.TemplateMetadata.Models.FundingLine;

namespace CalculateFunding.Services.Publishing
{
    public interface IPublishProviderExclusionCheck
    {
        PublishedProviderExclusionCheckResult ShouldBeExcluded(GeneratedProviderResult generatedProviderResult,
            TemplateFundingLine[] flattenedTemplateFundingLines);
    }
}