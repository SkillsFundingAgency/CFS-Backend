using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Generators.OrganisationGroup.Enums;
using CalculateFunding.Generators.OrganisationGroup.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Reporting.FundingLines;

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
        public IEnumerable<(PublishedFunding PublishedFunding, OrganisationGroupResult OrganisationGroupResult)> GenerateOrganisationGroupsToSave(IEnumerable<OrganisationGroupResult> organisationGroups, IEnumerable<PublishedFunding> existingPublishedFunding, IDictionary<string, PublishedProvider> currentPublishedProviders)
        {
            ConcurrentBag<(PublishedFunding, OrganisationGroupResult)> results = new ConcurrentBag<(PublishedFunding, OrganisationGroupResult)>();

            Parallel.ForEach(organisationGroups, (organisationGroup) =>
            {
                // get all funding where the organisation group matches the published funding
                PublishedFunding publishedFunding = existingPublishedFunding?.Where(_ => organisationGroup.IdentifierValue == _.Current.OrganisationGroupIdentifierValue &&
                organisationGroup.GroupTypeCode == Enum.Parse<OrganisationGroupTypeCode>(_.Current.OrganisationGroupTypeCode) &&
                organisationGroup.GroupTypeClassification == Enum.Parse<OrganisationGroupTypeClassification>(_.Current.OrganisationGroupTypeClassification) &&
                organisationGroup.GroupTypeIdentifier == Enum.Parse<OrganisationGroupTypeIdentifier>(_.Current.OrganisationGroupTypeIdentifier)
                ).OrderBy(_ => _.Current.Version).LastOrDefault();

                // no existing published funding so need to yield the organisation group
                if (publishedFunding == null || publishedFunding.Current == null)
                {
                    results.Add((null, organisationGroup));
                    return;
                }
                else
                {
                    // get all new funding where providers match providers in organisation group
                    IEnumerable<string> currentProviderFundings = organisationGroup.Providers.IsNullOrEmpty() ? Enumerable.Empty<string>() :
                    organisationGroup.Providers.Where(provider => currentPublishedProviders.ContainsKey(provider.ProviderId))
                    .Select(_ => currentPublishedProviders[_.ProviderId].Current.FundingId);

                    // get all current funding where the funding does not exist in the new funding
                    IEnumerable<string> fundingProviderMissing = currentProviderFundings.IsNullOrEmpty() ? publishedFunding.Current.ProviderFundings : publishedFunding.Current.ProviderFundings?.Where(_ => !currentProviderFundings.Any(current => _ == current));

                    // get all new funding where the funding id does not exist in the current funding
                    IEnumerable<string> currentFundingProviderMissing = publishedFunding.Current.ProviderFundings.IsNullOrEmpty() ? currentProviderFundings : currentProviderFundings?.Where(_ => !publishedFunding.Current.ProviderFundings.Any(current => _ == current));

                    if ((fundingProviderMissing?.Any() ?? false) || (currentFundingProviderMissing?.Any() ?? false))
                    {
                        results.Add((publishedFunding, organisationGroup));
                        return;
                    }
                }
            });

            return results;
        }

        public IEnumerable<PublishedFundingOrganisationGrouping> GenerateOrganisationGroupings(
            IEnumerable<OrganisationGroupResult> organisationGroups, 
            IEnumerable<PublishedFundingVersion> existingPublishedFunding, 
            bool includeNonCurrent)
        {
            ConcurrentBag<PublishedFundingOrganisationGrouping> results = new ConcurrentBag<PublishedFundingOrganisationGrouping>();

            Parallel.ForEach(organisationGroups, (organisationGroup) =>
            {
                // get all funding where the organisation group matches the published funding
                IEnumerable<PublishedFundingVersion> publishedFundingVersions = existingPublishedFunding?.Where(_ => organisationGroup.IdentifierValue == _.OrganisationGroupIdentifierValue &&
                organisationGroup.GroupTypeCode == Enum.Parse<OrganisationGroupTypeCode>(_.OrganisationGroupTypeCode) &&
                organisationGroup.GroupTypeClassification == Enum.Parse<OrganisationGroupTypeClassification>(_.OrganisationGroupTypeClassification) &&
                organisationGroup.GroupTypeIdentifier == Enum.Parse<OrganisationGroupTypeIdentifier>(_.OrganisationGroupTypeIdentifier)
                ).OrderBy(_ => _.Version);

                if (!includeNonCurrent)
                {
                    publishedFundingVersions = new[] { publishedFundingVersions.LastOrDefault() };
                }

                results.Add(new PublishedFundingOrganisationGrouping { OrganisationGroupResult = organisationGroup, PublishedFundingVersions = publishedFundingVersions });
                return;
            });

            return results;
        }
    }
}
