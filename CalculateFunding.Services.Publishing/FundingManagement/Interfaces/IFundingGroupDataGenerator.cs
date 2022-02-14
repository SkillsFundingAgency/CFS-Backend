using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Generators.OrganisationGroup.Models;
using CalculateFunding.Services.Publishing.FundingManagement.SqlModels;
using CalculateFunding.Services.Publishing.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.FundingManagement.Interfaces
{
    public interface IFundingGroupDataGenerator
    {
        Task<IEnumerable<GeneratedPublishedFunding>> Generate(IEnumerable<OrganisationGroupResult> organisationGroupsToCreate, SpecificationSummary specification, Channel channel, IEnumerable<string> batchPublishedProviderIds);
    }
}
