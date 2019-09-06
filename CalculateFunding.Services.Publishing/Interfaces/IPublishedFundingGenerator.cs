using System.Collections.Generic;
using CalculateFunding.Common.TemplateMetadata.Models;
using CalculateFunding.Generators.OrganisationGroup.Models;
using CalculateFunding.Models.Publishing;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IPublishedFundingGenerator
    {
        IEnumerable<PublishedFundingVersion> GeneratePublishedFunding(IEnumerable<OrganisationGroupResult> organisationGroupsToSave, TemplateMetadataContents templateMetadataContents, IEnumerable<PublishedProvider> publishedProviders);
    }
}
