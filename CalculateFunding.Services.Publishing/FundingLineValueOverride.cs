using System.Linq;
using CalculateFunding.Models.Publishing;

namespace CalculateFunding.Services.Publishing
{
    public class FundingLineValueOverride : IFundingLineValueOverride
    {
        public bool HasPreviousFunding(GeneratedProviderResult generatedProviderResult, PublishedProviderVersion publishedProviderVersion)
        {
            var hasPreviousFunding = false;

            foreach (FundingLine fundingLine in generatedProviderResult?.FundingLines?.Where(_ => _.Type == FundingLineType.Payment)
                                                ?? new FundingLine[0])
            {
                FundingLine previousFundingLineVersion =
                    publishedProviderVersion.FundingLines?.SingleOrDefault(_ => _.TemplateLineId == fundingLine.TemplateLineId);

                if (previousFundingLineVersion == null) continue;

                // only set to true if there is a previous funding value
                if (previousFundingLineVersion.Value.HasValue) hasPreviousFunding = true;
            }

            return hasPreviousFunding;
        }

        public void OverridePreviousFundingLineValues(PublishedProvider publishedProvider,
            GeneratedProviderResult generatedProviderResult)
        {
            PublishedProviderVersion publishedProviderVersion = publishedProvider.Current;

            foreach (FundingLine fundingLine in generatedProviderResult.FundingLines?.Where(_ => _.Type == FundingLineType.Payment && _.Value == null)
                                                ?? new FundingLine[0])
            {
                FundingLine previousFundingLineVersion =
                    publishedProviderVersion.FundingLines?.SingleOrDefault(_ => _.TemplateLineId == fundingLine.TemplateLineId);

                // only zero funding line if the provider has been released
                if (previousFundingLineVersion != null && previousFundingLineVersion.Value.HasValue && publishedProvider.Released != null) fundingLine.Value = 0M;
            }
        }
    }
}