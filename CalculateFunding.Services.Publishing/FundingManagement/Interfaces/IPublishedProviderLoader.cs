using CalculateFunding.Generators.OrganisationGroup.Models;
using CalculateFunding.Models.Publishing;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.FundingManagement.Interfaces
{
    public interface IPublishedProviderLoaderForFundingGroupData
    {
        Task<List<PublishedProvider>> GetAllPublishedProviders(IEnumerable<OrganisationGroupResult> organisationGroupsToCreate, string specificationId, int channelId, IEnumerable<string> batchPublishedProviderIds);
    }
}
