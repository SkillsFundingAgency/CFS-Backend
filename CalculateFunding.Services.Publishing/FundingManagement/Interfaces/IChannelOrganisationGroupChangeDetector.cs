using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Generators.OrganisationGroup.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.FundingManagement.SqlModels;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.FundingManagement.Interfaces
{
    public interface IChannelOrganisationGroupChangeDetector
    {
        Task<(IEnumerable<OrganisationGroupResult>, Dictionary<string, PublishedProviderVersion>)> DetermineFundingGroupsToCreateBasedOnProviderVersions(
            IEnumerable<OrganisationGroupResult> channelOrganisationGroups, Dictionary<string, PublishedProviderVersion> providersToRelease, SpecificationSummary specification, Channel channel);
    }
}