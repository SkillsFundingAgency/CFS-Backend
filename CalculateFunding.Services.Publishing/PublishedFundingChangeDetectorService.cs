using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Generators.OrganisationGroup.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;

namespace CalculateFunding.Services.Publishing
{
    public class PublishedFundingChangeDetectorService : IPublishedFundingChangeDetectorService
    {
        /// <summary>
        /// Generates the organisation group results which do not currently exist, or whose providers have updated since the last save
        /// </summary>
        /// <param name="organisationGroups"></param>
        /// <param name="existingPublishedFunding"></param>
        /// <param name="currentPublishedProviders"></param>
        /// <returns></returns>
        public Task<IEnumerable<OrganisationGroupResult>> GenerateOrganisationGroupsToSave(IEnumerable<OrganisationGroupResult> organisationGroups, IEnumerable<PublishedFunding> existingPublishedFunding, IEnumerable<PublishedProvider> currentPublishedProviders)
        {
            foreach (var organisationGroup in organisationGroups)
            {
                // TODO ultimaltely end up comparing the PublishedProvider.FundingId's already existing with the ones generated
            }

            throw new NotImplementedException();
        }
    }
}
