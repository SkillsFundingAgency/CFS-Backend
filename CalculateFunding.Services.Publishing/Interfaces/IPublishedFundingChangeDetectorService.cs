using System.Collections.Generic;
using CalculateFunding.Generators.OrganisationGroup.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Reporting.FundingLines;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IPublishedFundingChangeDetectorService
    {
        IEnumerable<(PublishedFunding PublishedFunding, OrganisationGroupResult OrganisationGroupResult)> GenerateOrganisationGroupsToSave(IEnumerable<OrganisationGroupResult> organisationGroups, IEnumerable<PublishedFunding> existingPublishedFunding, IDictionary<string, PublishedProvider> currentPublishedProviders);

        IEnumerable<PublishedFundingOrganisationGrouping> GenerateOrganisationGroupings(
            IEnumerable<OrganisationGroupResult> organisationGroups, 
            IEnumerable<PublishedFundingVersion> existingPublishedFunding, 
            bool includeHistory);
    }
}
