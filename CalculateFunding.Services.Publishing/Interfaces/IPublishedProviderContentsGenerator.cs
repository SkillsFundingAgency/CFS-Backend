using System.Collections.Generic;
using CalculateFunding.Common.TemplateMetadata.Models;
using CalculateFunding.Models.Publishing;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IPublishedProviderContentsGenerator
    {
        /// <summary>
        /// Generate contents for a Provider for a specific version of a schema.
        /// This file will be saved by CFS as the details of this provider version (usually in json format into BLOB storage)
        /// </summary>
        /// <param name="publishedProviderVersion">Published Provider Version</param>
        /// <param name="templateMetadataContents">Template contents</param>
        /// <param name="calculationResults">Calculation Results</param>
        /// <param name="fundingLines">Funding Lines (with precalculated totals). Should contain all funding lines, not just payment</param>
        /// <returns>Contents to be saved for this provider version</returns>
        string GenerateContents(PublishedProviderVersion publishedProviderVersion, TemplateMetadataContents templateMetadataContents, IEnumerable<CalculationResult> calculationResults, IEnumerable<Models.Publishing.FundingLine> fundingLines);
    }
}
