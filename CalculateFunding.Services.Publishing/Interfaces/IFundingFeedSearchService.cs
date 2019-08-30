using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Models.Results.Search;
using CalculateFunding.Models.Search;

namespace CalculateFunding.Services.Publising.Interfaces
{
    public interface IFundingFeedSearchService
    {
		Task<SearchFeedV3<PublishedFundingIndex>> GetFeedsV3(int? pageRef, 
            int top, 
            IEnumerable<string> fundingStreamIds = null, 
            IEnumerable<string> fundingPeriodIds = null, 
            IEnumerable<string> groupingReasons = null,
            params string[] orderBy);
    }
}
