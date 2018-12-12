using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Models.Results.Search;
using CalculateFunding.Models.Search;

namespace CalculateFunding.Services.Results.Interfaces
{
    public interface IAllocationNotificationsFeedsSearchService
    {
        Task<SearchFeed<AllocationNotificationFeedIndex>> GetFeeds(int pageRef, int top = 500, IEnumerable<string> statuses = null);

        Task<SearchFeed<AllocationNotificationFeedIndex>> GetFeeds(string providerId, int startYear, int endYear, IEnumerable<string> allocationLineIds);

        Task<SearchFeed<AllocationNotificationFeedIndex>> GetLocalAuthorityFeeds(string laCode, int startYear, int endYear, IEnumerable<string> customFilters);
    }
}
