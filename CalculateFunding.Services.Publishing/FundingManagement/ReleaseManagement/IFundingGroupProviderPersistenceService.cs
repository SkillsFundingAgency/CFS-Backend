using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.FundingManagement.SqlModels;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.FundingManagement.ReleaseManagement
{
    public interface IFundingGroupProviderPersistenceService
    {
        Task PersistFundingGroupProviders(int channelId, IEnumerable<(PublishedFundingVersion, Generators.OrganisationGroup.Models.OrganisationGroupResult)> fundingGroupData, IEnumerable<PublishedProviderVersion> providersInGroupsToRelease);
    }
}