using CalculateFunding.Api.External.V4.Models;
using CalculateFunding.Models.External;
using CalculateFunding.Models.External.V4;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Api.External.V4.Interfaces
{
    public interface IFundingFeedServiceV4
    {
        Task<ActionResult<SearchFeedResult<ExternalFeedFundingGroupItem>>> GetFundingNotificationFeedPage(
            HttpRequest request,
            HttpResponse response,
            int? pageRef,
            string channelUrlKey,
            IEnumerable<string> fundingStreamIds,
            IEnumerable<string> fundingPeriodIds,
            IEnumerable<GroupingReason> groupingReasons,
            IEnumerable<VariationReason> variationReasons,
            int? pageSize,
            System.Threading.CancellationToken cancellationToken);
    }
}
