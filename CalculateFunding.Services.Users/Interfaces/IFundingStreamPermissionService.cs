using System.Threading.Tasks;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Users;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.ServiceBus;

namespace CalculateFunding.Services.Users.Interfaces
{
    public interface IFundingStreamPermissionService
    {
        /// <summary>
        /// Update Funding Stream permissions for a user
        /// </summary>
        /// <param name="userId">User ID (AAD Object ID)</param>
        /// <param name="fundingStreamId">Funding Stream ID</param>
        /// <returns></returns>
        Task<IActionResult> UpdatePermissionForUser(string userId, string fundingStreamId, FundingStreamPermissionUpdateModel fundingStreamPermissionUpdateModel, Reference user);

        /// <summary>
        /// Gets the effective permission for a user given a particular specification
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="specificationId">Specification Id</param>
        /// <returns></returns>
        Task<IActionResult> GetEffectivePermissionsForUser(string userId, string specificationId);

        /// <summary>
        /// Get permissions for User
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>IEnumerable of Funding Stream Permissions</returns>
        Task<IActionResult> GetFundingStreamPermissionsForUser(string userId);

        /// <summary>
        /// Triggered when a specification is updated. This method should check for differences in Funding Streams and update the effective permissions for users
        /// </summary>
        /// <param name="message">Message</param>
        /// <returns>Task</returns>
        Task OnSpecificationUpdate(Message message);
    }
}
