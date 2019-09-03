using System.Collections.Generic;
using CalculateFunding.Common.TemplateMetadata.Models;
using CalculateFunding.Models.Publishing;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IFundingLineGenerator
    {
        /// <summary>
        /// Generate funding lines for publishing
        /// </summary>
        /// <param name="templateMetadata">Template Metadata Dictionary. Keyed on fundingStreamId</param>
        /// <param name="templateMapping">Template Mapping</param>
        /// <param name="scopedProviders">Scoped providers for this specification</param>
        /// <param name="calculationResults">Calculation Results for Specification</param>
        /// <returns>Dictionary of providers (provider ID as key) containing the funding streams, calculations and reference data of that provider</returns>
        Dictionary<string, GeneratedProviderResult> GenerateFundingLines(TemplateMetadataContents templateMetadata, Common.ApiClient.Calcs.Models.TemplateMapping templateMapping, IEnumerable<Common.ApiClient.Providers.Models.Provider> scopedProviders, IEnumerable<ProviderCalculationResult> calculationResults);
    }
}
