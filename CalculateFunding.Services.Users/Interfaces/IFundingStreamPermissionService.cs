using System.Threading.Tasks;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Users;
using CalculateFunding.Services.Processing.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Services.Users.Interfaces
{
    public interface IFundingStreamPermissionService : IProcessingService
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
        /// Gets the download URL for users on given a funding stream
        /// </summary>
        /// <param name="fundingStreamId">Funding Stream ID</param>
        /// <returns></returns>
        Task<IActionResult> DownloadEffectivePermissionsForFundingStream(string fundingStreamId);

        /// <summary>
        /// Gets the admin user details on given a funding stream
        /// </summary>
        /// <param name="fundingStreamId">Funding Stream ID</param>
        /// <returns></returns>
        Task<IActionResult> GetAdminUsersForFundingStream(string fundingStreamId);
    }
}
