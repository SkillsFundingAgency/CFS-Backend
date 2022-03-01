using CalculateFunding.Repositories.Common.Search.Results;
using CalculateFunding.Services.Publishing.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.FundingManagement.Interfaces
{
    public interface IPublishedProvidersSearchService
    {
        Task<Dictionary<string, IEnumerable<ReleaseChannel>>> GetPublishedProviderReleaseChannelsLookup(ReleaseChannelSearch searchRequest);
    }
}
