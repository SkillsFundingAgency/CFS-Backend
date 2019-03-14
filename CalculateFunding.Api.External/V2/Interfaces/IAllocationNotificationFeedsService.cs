using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Api.External.V2.Interfaces
{
    public interface IAllocationNotificationFeedsService
    {
        Task<IActionResult> GetNotifications(HttpRequest request, int? pageRef, int? startYear, int? endYear, IEnumerable<string> fundingStreamIds, IEnumerable<string> allocationLineIds, IEnumerable<string> allocationStatuses, IEnumerable<string> ukprns, IEnumerable<string> laCodes, bool? isAllocationLineContractRequired, int? pageSize);
    }
}
