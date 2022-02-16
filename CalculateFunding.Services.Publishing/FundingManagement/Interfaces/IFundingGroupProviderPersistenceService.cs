using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.FundingManagement.ReleaseManagement
{
    public interface IFundingGroupProviderPersistenceService
    {
        Task PersistFundingGroupProviders(int channelId, IEnumerable<GeneratedPublishedFunding> fundingGroupData, IEnumerable<PublishedProviderVersion> providersInGroupsToRelease);
    }
}