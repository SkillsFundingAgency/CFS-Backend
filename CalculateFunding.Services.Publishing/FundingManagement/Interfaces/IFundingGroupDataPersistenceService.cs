using CalculateFunding.Generators.OrganisationGroup.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.FundingManagement.SqlModels;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.FundingManagement.Interfaces
{
    public interface IFundingGroupDataPersistenceService
    {
        Task<IEnumerable<FundingGroupVersion>> ReleaseFundingGroupData(IEnumerable<(PublishedFundingVersion, OrganisationGroupResult)> fundingGroupData, int channelId);
    }
}
