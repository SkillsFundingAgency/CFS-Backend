using CalculateFunding.Generators.OrganisationGroup.Models;
using CalculateFunding.Models.Publishing;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.FundingManagement.Interfaces
{
    public interface IFundingGroupDataPersistenceService
    {
        Task ReleaseFundingGroupData(IEnumerable<(PublishedFundingVersion, OrganisationGroupResult)> fundingGroupData, int channelId);
    }
}
