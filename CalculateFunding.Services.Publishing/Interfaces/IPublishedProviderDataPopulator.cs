using System.Collections.Generic;
using CalculateFunding.Common.ApiClient.Providers.Models;
using CalculateFunding.Common.TemplateMetadata.Models;
using CalculateFunding.Models.Publishing;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IPublishedProviderDataPopulator
    {
        /// <summary>
        /// Update Calculations
        /// </summary>
        /// <param name="publishedProvider">Published provider</param>
        /// <param name="templateMetadataContents">Template Metadata Contents</param>
        /// <param name="calculationResults">Calculation Results</param>
        /// <returns></returns>
        bool UpdateCalculations(PublishedProvider publishedProvider, TemplateMetadataContents templateMetadataContents, IEnumerable<CalculationResult> calculationResults);

        /// <summary>
        /// Update Funding Lines
        /// </summary>
        /// <param name="publishedProvider">Published Provider</param>
        /// <param name="fundingLines">Funding Lines</param>
        /// <returns></returns>
        bool UpdateFundingLines(PublishedProvider publishedProvider, IEnumerable<Models.Publishing.FundingLine> fundingLines);
        bool UpdateProfiling(PublishedProvider value, IEnumerable<Models.Publishing.FundingLine> enumerable);
        bool UpdateProviderInformation(PublishedProvider value, Common.ApiClient.Providers.Models.Provider provider);
    }
}
