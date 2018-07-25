using CalculateFunding.Models.Results;
using CalculateFunding.Models.Search;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Results.Interfaces
{
    public interface IAllocationNotificationsFeedsSearchService
    {
        Task<SearchFeed<AllocationNotificationFeedIndex>> GetFeeds(int pageRef, int top = 500, IEnumerable<string> statuses = null);
    }
}
