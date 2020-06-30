using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Models.External;
using CalculateFunding.Models.Publishing;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IFundingFeedSearchService
    {
		Task<SearchFeedV3<PublishedFundingIndex>> GetFeedsV3(int? pageRef, 
            int top, 
            IEnumerable<string> fundingStreamIds = null, 
            IEnumerable<string> fundingPeriodIds = null, 
            IEnumerable<string> groupingReasons = null,
            IEnumerable<string> variationReasons = null);
    }
}
