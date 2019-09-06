using System;
using System.Collections.Generic;
using CalculateFunding.Common.TemplateMetadata.Models;
using CalculateFunding.Generators.OrganisationGroup.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;

namespace CalculateFunding.Services.Publishing
{
    public class PublishedFundingGenerator : IPublishedFundingGenerator
    {
        /// <summary>
        /// Generate instances of the PublishedFundingVersion to save into cosmos for the Organisation Group Results
        /// </summary>
        /// <param name="organisationGroupsToSave"></param>
        /// <param name="templateMetadataContents"></param>
        /// <param name="publishedProviders"></param>
        /// <returns></returns>
        public IEnumerable<PublishedFundingVersion> GeneratePublishedFunding(IEnumerable<OrganisationGroupResult> organisationGroupsToSave, TemplateMetadataContents templateMetadataContents, IEnumerable<PublishedProvider> publishedProviders)
        {
            throw new NotImplementedException();
        }
    }
}
