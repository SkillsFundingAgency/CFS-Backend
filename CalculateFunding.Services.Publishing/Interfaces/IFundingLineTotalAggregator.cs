using System.Collections.Generic;
using CalculateFunding.Common.ApiClient.Calcs.Models;
using CalculateFunding.Common.TemplateMetadata.Models;
using GeneratorModels = CalculateFunding.Generators.Funding.Models;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IFundingLineTotalAggregator
    {
        GeneratorModels.FundingValue GenerateTotals(TemplateMetadataContents templateMetadataContents, TemplateMapping mapping, IEnumerable<CalculateFunding.Models.Publishing.CalculationResult> calculationResults);
    }
}
