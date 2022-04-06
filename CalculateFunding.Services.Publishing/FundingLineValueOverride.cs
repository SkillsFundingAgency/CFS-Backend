using System;
using System.Collections.Generic;
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
                                                ?? System.Array.Empty<FundingLine>())
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
            PublishedProviderVersion releasedPublishedProviderVersion = publishedProvider.Released;

            IEnumerable<FundingLine> paymentFundingLines = generatedProviderResult.FundingLines?.Where(_ => _.Type == FundingLineType.Payment);

            if (releasedPublishedProviderVersion == null || 
                releasedPublishedProviderVersion.FundingLines.IsNullOrEmpty() || 
                paymentFundingLines.IsNullOrEmpty()) return;

            foreach (FundingLine fundingLine in paymentFundingLines.Where(_ => _.Value == null))
            {
                FundingLine releasedFundingLineVersion =
                    releasedPublishedProviderVersion.FundingLines.SingleOrDefault(_ => _.TemplateLineId == fundingLine.TemplateLineId);

                // only zero funding line if it has previously been released with a value
                if (releasedFundingLineVersion != null && releasedFundingLineVersion.Value.HasValue)
                {
                    fundingLine.Value = 0M;
                }
            }

            // if any payment funding lines have a value and all of them are zero or null then set total funding to zero
            if (paymentFundingLines.AnyWithNullCheck(_ => _.Value.HasValue) &&
                paymentFundingLines.All(_ => _.Value.GetValueOrDefault() == 0))
            {
                generatedProviderResult.TotalFunding = 0M;
            }
        }
    }
}