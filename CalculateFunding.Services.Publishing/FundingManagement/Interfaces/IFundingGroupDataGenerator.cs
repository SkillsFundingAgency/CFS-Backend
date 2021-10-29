using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Generators.OrganisationGroup.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.FundingManagement.SqlModels;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.FundingManagement.Interfaces
{
    public interface IFundingGroupDataGenerator
    {
        Task<IEnumerable<(PublishedFundingVersion, OrganisationGroupResult)>> Generate(IEnumerable<OrganisationGroupResult> organisationGroupsToCreate, SpecificationSummary specification, Channel channel, IEnumerable<string> batchPublishedProviderIds);
    }
}
