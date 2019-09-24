using CalculateFunding.Models.Publishing;

namespace CalculateFunding.Services.Publishing
{
    public interface IFundingLineValueOverride
    {
        bool TryOverridePreviousFundingLineValues(PublishedProviderVersion publishedProviderVersion,
            GeneratedProviderResult generatedProviderResult);
    }
}