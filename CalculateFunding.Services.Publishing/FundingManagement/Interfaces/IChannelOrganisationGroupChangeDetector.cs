using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Generators.OrganisationGroup.Models;
using CalculateFunding.Services.Publishing.FundingManagement.SqlModels;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.FundingManagement.Interfaces
{
    public interface IChannelOrganisationGroupChangeDetector
    {
        Task<IEnumerable<OrganisationGroupResult>> DetermineFundingGroupsToCreateBasedOnProviderVersions(IEnumerable<OrganisationGroupResult> channelOrganisationGroups, SpecificationSummary specification, Channel channel);
    }
}