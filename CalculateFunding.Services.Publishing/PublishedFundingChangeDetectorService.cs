using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Generators.OrganisationGroup.Enums;
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
        public IEnumerable<(PublishedFunding PublishedFunding, OrganisationGroupResult OrganisationGroupResult)> GenerateOrganisationGroupsToSave(IEnumerable<OrganisationGroupResult> organisationGroups, IEnumerable<PublishedFunding> existingPublishedFunding, IEnumerable<PublishedProvider> currentPublishedProviders)
        {
            foreach (var organisationGroup in organisationGroups)
            {
                // get all funding where the organisation group matches the published funding
                PublishedFunding publishedFunding = existingPublishedFunding?.Where(_ => organisationGroup.GroupTypeCode == Enum.Parse<OrganisationGroupTypeCode>(_.Current.OrganisationGroupTypeCode) && 
                organisationGroup.GroupTypeClassification == Enum.Parse<OrganisationGroupTypeClassification>(_.Current.OrganisationGroupTypeCategory) && 
                organisationGroup.GroupTypeIdentifier == Enum.Parse<OrganisationGroupTypeIdentifier>(_.Current.OrganisationGroupTypeIdentifier) && 
                organisationGroup.IdentifierValue == _.Current.OrganisationGroupIdentifierValue).OrderBy(_ => _.Current.Version).LastOrDefault();

                // no existing published funding so need to yield the organisation group
                if (publishedFunding == null || publishedFunding.Current == null)
                {
                    yield return (null, organisationGroup);
                }
                else
                {
                    // get all new funding where providers match providers in organisation group
                    IEnumerable<string> currentProviderFundings = currentPublishedProviders?.Where(_ => organisationGroup.Providers.IsNullOrEmpty() ? false : organisationGroup.Providers.Where(provider => provider.ProviderId == _.Current.ProviderId).Any()).Select(_ => _.Current.FundingId);

                    // get all current funding where the funding does not exist in the new funding
                    IEnumerable<string> fundingProviderMissing = currentProviderFundings.IsNullOrEmpty() ? publishedFunding.Current.ProviderFundings : publishedFunding.Current.ProviderFundings?.Where(_ => !currentProviderFundings.Any(current => _ == current));

                    // get all new funding where the funding id does not exist in the current funding
                    IEnumerable<string> currentFundingProviderMissing = publishedFunding.Current.ProviderFundings.IsNullOrEmpty() ? currentProviderFundings : currentProviderFundings?.Where(_ => !publishedFunding.Current.ProviderFundings.Any(current => _ == current));

                    if ((fundingProviderMissing?.Any() ?? false) || (currentFundingProviderMissing?.Any() ?? false))
                    {
                        yield return (publishedFunding, organisationGroup);
                    }
                }
            }
        }
    }
}
