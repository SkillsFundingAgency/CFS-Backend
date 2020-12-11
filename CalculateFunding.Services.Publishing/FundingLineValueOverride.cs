using System.Linq;
using CalculateFunding.Models.Publishing;

namespace CalculateFunding.Services.Publishing
{
    public class FundingLineValueOverride : IFundingLineValueOverride
    {
        public bool TryOverridePreviousFundingLineValues(PublishedProviderVersion publishedProviderVersion,
            GeneratedProviderResult generatedProviderResult)
        {
            var haveZeroedNullPaymentLine = false;

            foreach (FundingLine fundingLine in generatedProviderResult.FundingLines?.Where(_ => _.Type == FundingLineType.Payment)
                                                ?? new FundingLine[0])
            {
                FundingLine previousFundingLineVersion =
                    publishedProviderVersion.FundingLines?.SingleOrDefault(_ => _.TemplateLineId == fundingLine.TemplateLineId);

                if (previousFundingLineVersion == null) continue;

                fundingLine.Value = 0M;
                haveZeroedNullPaymentLine = true;
            }

            return haveZeroedNullPaymentLine;
        }
    }
}