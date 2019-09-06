using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Generators.OrganisationGroup.Models;
using CalculateFunding.Models.Publishing;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IPublishedFundingChangeDetectorService
    {
        Task<IEnumerable<OrganisationGroupResult>> GenerateOrganisationGroupsToSave(IEnumerable<OrganisationGroupResult> organisationGroups, IEnumerable<PublishedFunding> existingPublishedFunding, IEnumerable<PublishedProvider> currentPublishedProviders);
    }
}
