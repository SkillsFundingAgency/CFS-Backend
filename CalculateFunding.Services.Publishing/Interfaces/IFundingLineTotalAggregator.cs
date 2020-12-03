using System.Collections.Generic;
using CalculateFunding.Common.ApiClient.Calcs.Models;
using TemplateModels = CalculateFunding.Common.TemplateMetadata.Models;
using CalculationResult = CalculateFunding.Models.Publishing.CalculationResult;
using GeneratorModels = CalculateFunding.Generators.Funding.Models;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IFundingLineTotalAggregator
    {
        GeneratorModels.FundingValue GenerateTotals(TemplateModels.TemplateMetadataContents templateMetadataContents, IDictionary<uint, TemplateMappingItem> mappingItems, IDictionary<string, CalculationResult> calculationResults);
    }
}
