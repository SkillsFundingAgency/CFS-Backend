using CalculateFunding.Models.Publishing;

namespace CalculateFunding.Services.Publishing
{
    public interface IPublishProviderExclusionCheck
    {
        PublishedProviderExclusionCheckResult ShouldBeExcluded(string providerId, GeneratedProviderResult generatedProviderResult, Common.TemplateMetadata.Models.FundingLine[] flattenedTemplateFundingLines);
    }
}