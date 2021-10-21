using CalculateFunding.Generators.OrganisationGroup.Models;
using CalculateFunding.Services.Publishing.FundingManagement.SqlModels;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.FundingManagement.Interfaces
{
    public interface IFundingGroupService
    {
        Task<IEnumerable<FundingGroup>> CreateFundingGroups(string specificationId, int channelId, IEnumerable<OrganisationGroupResult> organisationGroupResults);
    }
}
