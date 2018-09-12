using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace CalculateFunding.Api.External.V1.Interfaces
{
    public interface IAllocationNotificationFeedsService
    {
        Task<IActionResult> GetNotifications(int? pageRef, string allocationStatuses, int? pageSize, HttpRequest request);
    }
}
