using CalculateFunding.Common.ApiClient.Policies.Models.FundingConfig;
using CalculateFunding.Generators.OrganisationGroup.Models;
using CalculateFunding.Models.Publishing;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IOrganisationGroupService
    {
        Task<Dictionary<string, IEnumerable<OrganisationGroupResult>>> GenerateOrganisationGroups(IEnumerable<Provider> scopedProviders, IEnumerable<PublishedProvider> publishedProviders, FundingConfiguration fundingConfiguration, string providerVersionId, int? providerSnapshotId = null);
    }
}