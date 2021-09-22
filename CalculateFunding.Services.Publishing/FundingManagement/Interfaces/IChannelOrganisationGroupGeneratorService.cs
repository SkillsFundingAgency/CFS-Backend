using CalculateFunding.Common.ApiClient.Policies.Models.FundingConfig;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Generators.OrganisationGroup.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.FundingManagement.SqlModels;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.FundingManagement.Interfaces
{
    public interface IChannelOrganisationGroupGeneratorService
    {
        Task<IEnumerable<OrganisationGroupResult>> GenerateOrganisationGroups(Channel channel, FundingConfiguration fundingConfiguration, SpecificationSummary specification, IEnumerable<PublishedProviderVersion> publishedProvidersInReleaseBatch);
    }
}