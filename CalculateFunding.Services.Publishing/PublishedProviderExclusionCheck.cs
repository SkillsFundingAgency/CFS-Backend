using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Models.Publishing;

namespace CalculateFunding.Services.Publishing
{
    public class PublishedProviderExclusionCheck : IPublishProviderExclusionCheck
    {
        public PublishedProviderExclusionCheckResult ShouldBeExcluded(
            string providerId,
            GeneratedProviderResult generatedProviderResult, 
            Common.TemplateMetadata.Models.FundingLine[] flattenedTemplateFundingLines)
        {
            bool shouldBeExcluded = true;

            if (generatedProviderResult != null)
            {
                IEnumerable<FundingLine> paymentFundingLines = generatedProviderResult.FundingLines?.Where(
                    x => x.Type == FundingLineType.Payment
                    && flattenedTemplateFundingLines.Any(y => y.TemplateLineId == x.TemplateLineId && y.Type == Common.TemplateMetadata.Enums.FundingLineType.Payment));

                shouldBeExcluded = !paymentFundingLines.Any() || paymentFundingLines.All(c => c.Value == null);
            }

            return new PublishedProviderExclusionCheckResult(providerId, shouldBeExcluded);
        }
    }
}