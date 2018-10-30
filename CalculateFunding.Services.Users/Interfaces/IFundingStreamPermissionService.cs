using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Services.Users.Interfaces
{
    public interface IFundingStreamPermissionService
    {
        Task<IActionResult> UpdatePermissionForUser(string userId, string fundingStreamId, HttpRequest request);

        Task<IActionResult> GetEffectivePermissionsForUser(string userId, string specificationId, HttpRequest request);

        /// <summary>
        /// Get permissions for User
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="request">HTTP Request</param>
        /// <returns>IEnumerable of Funding Stream Permissions</returns>
        Task<IActionResult> GetFundingStreamPermissionsForUser(string userId, HttpRequest request);
    }
}
