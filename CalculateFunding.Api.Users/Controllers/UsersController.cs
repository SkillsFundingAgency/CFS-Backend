using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Users;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Users.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Api.Users.Controllers
{
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IFundingStreamPermissionService _fundingStreamPermissionService;
        private readonly IUserIndexingService _userIndexingService;

        public UsersController(IUserService userService, IFundingStreamPermissionService fundingStreamPermissionService, IUserIndexingService userIndexingService)
        {
            Guard.ArgumentNotNull(userService, nameof(userService));
            Guard.ArgumentNotNull(fundingStreamPermissionService, nameof(fundingStreamPermissionService));
            Guard.ArgumentNotNull(userIndexingService, nameof(userIndexingService));

            _userService = userService;
            _fundingStreamPermissionService = fundingStreamPermissionService;
            _userIndexingService = userIndexingService;
        }

        [Route("api/users/confirm-skills")]
        [HttpPost]
        [Produces(typeof(User))]
        public async Task<IActionResult> RunConfirmSkills([FromQuery] string userId, [FromBody] UserCreateModel userCreateModel)
        {
            return await _userService.ConfirmSkills(userId, userCreateModel);
        }

        [Route("api/users/get-user-by-userid")]
        [HttpGet]
        [Produces(typeof(User))]
        public async Task<IActionResult> RunGetUserByUserId([FromQuery] string userId)
        {
            return await _userService.GetUserByUserId(userId);
        }

        [Route("api/users/{userId}/permissions")]
        [HttpGet]
        [Produces(typeof(IEnumerable<FundingStreamPermissionCurrent>))]
        public async Task<IActionResult> RunGetFundingStreamPermissionsForUser([FromRoute]string userId)
        {
            return await _fundingStreamPermissionService.GetFundingStreamPermissionsForUser(userId);
        }

        [Route("api/users/{userId}/permissions/{fundingStreamId}")]
        [HttpPut]
        [Produces(typeof(FundingStreamPermissionCurrent))]
        public async Task<IActionResult> RunUpdatePermissionForUser([FromRoute]string userId, [FromRoute]string fundingStreamId, [FromBody] FundingStreamPermissionUpdateModel fundingStreamPermissionUpdateModel)
        {
            Reference user = ControllerContext.HttpContext.Request.GetUser();

            return await _fundingStreamPermissionService.UpdatePermissionForUser(userId, fundingStreamId, fundingStreamPermissionUpdateModel, user);
        }

        [Route("api/users/{userid}/effectivepermissions/{specificationId}")]
        [HttpGet]
        [Produces(typeof(EffectiveSpecificationPermission))]
        public async Task<IActionResult> RunGetEffectivePermissionsForUser([FromRoute]string userId, [FromRoute]string specificationId)
        {
            return await _fundingStreamPermissionService.GetEffectivePermissionsForUser(userId, specificationId);
        }


        [Route("api/users/reindex")]
        [HttpGet]
        [ProducesResponseType(204)]
        public async Task<IActionResult> ReIndex()
        {
            return await _userIndexingService.ReIndex(Request.GetUser(), Request.GetCorrelationId());
        }

        [Route("api/users/effectivepermissions/generate-report/{fundingStreamId}")]
        [HttpGet]
        [Produces(typeof(FundingStreamPermissionCurrentDownloadModel))]
        public async Task<IActionResult> DownloadEffectivePermissionsForFundingStream([FromRoute] string fundingStreamId)
        {
            return await _fundingStreamPermissionService.DownloadEffectivePermissionsForFundingStream(fundingStreamId);
        }

        [Route("api/users/permissions/{fundingStreamId}/admin")]
        [HttpGet]
        [Produces(typeof(FundingStreamPermissionCurrent))]
        public async Task<IActionResult> GetAdminUsersForFundingStream([FromRoute] string fundingStreamId)
        {
            return await _fundingStreamPermissionService.GetAdminUsersForFundingStream(fundingStreamId);
        }
    }
}