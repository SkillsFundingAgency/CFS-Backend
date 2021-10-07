using CalculateFunding.Common.ApiClient.Providers.Models;
using CalculateFunding.Generators.OrganisationGroup.Models;
using System.Collections.Generic;
using System.Linq;

namespace CalculateFunding.Services.Publishing.FundingManagement.ReleaseManagement
{
    public static class OrganisationGroupResultExtensionMethods
    {
        public static IDictionary<string, IEnumerable<OrganisationGroupResult>> GroupByProviderId(this IEnumerable<OrganisationGroupResult> organisationGroupResults)
        {
            Dictionary<string, List<OrganisationGroupResult>> results = new Dictionary<string, List<OrganisationGroupResult>>();

            foreach (OrganisationGroupResult orgGroup in organisationGroupResults)
            {
                foreach (Provider provider in orgGroup.Providers)
                {
                    if (!results.TryGetValue(provider.ProviderId, out List<OrganisationGroupResult> providerOrganisationGroups))
                    {
                        providerOrganisationGroups = new List<OrganisationGroupResult>();
                        results.Add(provider.ProviderId, providerOrganisationGroups);
                    }

                    providerOrganisationGroups.Add(orgGroup);
                }
            }

            return results.ToDictionary(_ => _.Key, _ => _.Value.AsEnumerable());
        }
    }
}
