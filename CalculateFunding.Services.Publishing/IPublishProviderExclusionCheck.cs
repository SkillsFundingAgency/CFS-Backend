using CalculateFunding.Models.Publishing;

namespace CalculateFunding.Services.Publishing
{
    public interface IPublishProviderExclusionCheck
    {
        PublishedProviderExclusionCheckResult ShouldBeExcluded(GeneratedProviderResult generatedProviderResult, Common.TemplateMetadata.Models.FundingLine[] flattenedTemplateFundingLines);
    }
}