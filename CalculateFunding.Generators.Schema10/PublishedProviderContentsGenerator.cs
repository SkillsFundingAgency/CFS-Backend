using System.Collections.Generic;
using CalculateFunding.Common.TemplateMetadata.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;
using FundingLine = CalculateFunding.Models.Publishing.FundingLine;

namespace CalculateFunding.Generators.Schema10
{
    public class PublishedProviderContentsGenerator : IPublishedProviderContentsGenerator
    {
        public string GenerateContents(PublishedProviderVersion publishedProviderVersion, TemplateMetadataContents templateMetadataContents, IEnumerable<CalculationResult> calculationResults, IEnumerable<FundingLine> fundingLines)
        {
            throw new System.NotImplementedException();
        }
    }
}
