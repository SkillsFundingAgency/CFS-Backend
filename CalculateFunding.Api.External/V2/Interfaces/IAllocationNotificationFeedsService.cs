using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace CalculateFunding.Api.External.V2.Interfaces
{
    public interface IAllocationNotificationFeedsService
    {
        Task<IActionResult> GetNotifications(HttpRequest request, int? pageRef, int? startYear, int? endYear, string[] fundingStreamIds, string[] allocationLineIds, string[] allocationStatuses, string ukprn, string laCode, bool? isAllocationLineContractRequired, int? pageSize);
    }
}
