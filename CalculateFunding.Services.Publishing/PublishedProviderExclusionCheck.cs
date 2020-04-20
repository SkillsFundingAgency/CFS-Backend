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