using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Common.ApiClient.Calcs.Models;
using CalculateFunding.Models.Publishing;
using TemplateFundingLine = CalculateFunding.Common.TemplateMetadata.Models.FundingLine;

namespace CalculateFunding.Services.Publishing
{
    public class PublishedProviderExclusionCheck : IPublishProviderExclusionCheck
    {
        public PublishedProviderExclusionCheckResult ShouldBeExcluded(
            GeneratedProviderResult generatedProviderResult,
            TemplateFundingLine[] flattenedTemplateFundingLines)
        {
            var providerProviderId = generatedProviderResult.Provider.ProviderId;

            IEnumerable<FundingLine> fundingLines = generatedProviderResult.FundingLines?
                .Where(fundingLine => fundingLine.Value != null);

            if (fundingLines != null)
                foreach (FundingLine line in fundingLines)
                {
                    IEnumerable<uint> calculationTemplateIdsForFundingLine = flattenedTemplateFundingLines
                        .Where(fundingLine => fundingLine.TemplateLineId == line.TemplateLineId)
                        .SelectMany(fundingLine => fundingLine.Calculations.Flatten(cal => cal.Calculations))
                        .Select(calculation => calculation.TemplateCalculationId)
                        .Distinct()
                        .ToArray();

                    //if there are any none null totals for a payment funding line then we can NOT exclude this published provider
                    if (generatedProviderResult.Calculations
                        .Any(fundingCalculation => fundingCalculation.Value != null &&
                                  calculationTemplateIdsForFundingLine.Contains(fundingCalculation.TemplateCalculationId)))
                        return new PublishedProviderExclusionCheckResult(providerProviderId, false);
                }

            //All payment funding lines for this provider have null results
            return new PublishedProviderExclusionCheckResult(providerProviderId, true);
        }

        public PublishedProviderExclusionCheckResult ShouldBeExcluded(
            ProviderCalculationResult providerCalculationResult, 
            TemplateMapping templateMapping, 
            Common.TemplateMetadata.Models.Calculation[] flattedCalculations)
        {
            IEnumerable<uint> cashCalculationTemplateIds = flattedCalculations
                .Where(c => c.Type == Common.TemplateMetadata.Enums.CalculationType.Cash)
                .Select(f => f.TemplateCalculationId);

            IEnumerable<string> cashCalculationIds = templateMapping.TemplateMappingItems
                .Where(c => c.EntityType == TemplateMappingEntityType.Calculation 
                            && cashCalculationTemplateIds.Contains(c.TemplateId))
                .Select(f => f.CalculationId);

            bool shouldBeExcluded = providerCalculationResult.Results
                .Any(c => cashCalculationIds.Contains(c.Id) && c.Value == null);

            return new PublishedProviderExclusionCheckResult(providerCalculationResult.ProviderId, shouldBeExcluded);
        }
    }
}