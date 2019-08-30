using CalculateFunding.Common.TemplateMetadata.Models;
using CalculateFunding.Models.Publishing;
using System.Collections.Generic;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IPublishedFundingContentsGenerator
    {
        /// <summary>
        /// Generate the contents for a Published Funding entry on the feed
        /// </summary>
        /// <param name="publishedFundingVersion">Published Funding Version</param>
        /// <returns>Contents to output in the feed</returns>
        string GenerateContents(PublishedFundingVersion publishedFundingVersion, TemplateMetadataContents templateMetadataContents);
    }
}
