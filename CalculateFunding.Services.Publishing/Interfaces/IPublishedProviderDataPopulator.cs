using System.Collections.Generic;
using CalculateFunding.Common.TemplateMetadata.Models;
using CalculateFunding.Models.Publishing;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IPublishedProviderDataPopulator
    {
        /// <summary>
        /// Updates the given data on the Published Provider.
        /// This method is responsible for applying the data passed into on to the PublishedProviderVersion and returning if the PublishedProviderVersion has been updated
        /// </summary>
        /// <param name="publishedProviderVersion">Published Provider Version</param>
        /// <param name="fundingLines">Funding lines and profiling information</param>
        /// <param name="templateMetadataContents">Template Metadata Contents</param>
        /// <param name="calculationResults">Calculation Results</param>
        /// <param name="provider">Core provider information</param>
        /// <returns>True when the PublishedProviderVersion has been updated, false if not</returns>
        bool UpdatePublishedProvider(PublishedProviderVersion publishedProviderVersion, IEnumerable<Models.Publishing.FundingLine> fundingLines, TemplateMetadataContents templateMetadataContents, IEnumerable<CalculationResult> calculationResults, Common.ApiClient.Providers.Models.Provider provider);
    }
}
