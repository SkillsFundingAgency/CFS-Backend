using CalculateFunding.Api.External.V3.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Api.External.V3.Interfaces
{
    public interface IFundingFeedService
    {
        Task<IActionResult> GetFunding(
            HttpRequest request,
            HttpResponse response,
            int? pageRef,
            IEnumerable<string> fundingStreamIds,
            IEnumerable<string> fundingPeriodIds,
            IEnumerable<GroupingReason> groupingReasons,
            IEnumerable<VariationReason> variationReasons,
            int? pageSize);
    }
}
