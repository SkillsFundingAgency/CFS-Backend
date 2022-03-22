using CalculateFunding.Models.Publishing;

namespace CalculateFunding.Services.Publishing
{
    public interface IFundingLineValueOverride
    {
        bool HasPreviousFunding(GeneratedProviderResult generatedProviderResult,
            PublishedProviderVersion publishedProviderVersion);

        void OverridePreviousFundingLineValues(PublishedProvider publishedProvider,
            GeneratedProviderResult generatedProviderResult);
    }
}