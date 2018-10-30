using System.Threading.Tasks;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Users.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Api.Users.Controllers
{
    public class UsersController : Controller
    {
        private readonly IUserService _userService;
        private readonly IFundingStreamPermissionService _fundingStreamPermissionService;

        public UsersController(IUserService userService, IFundingStreamPermissionService fundingStreamPermissionService)
        {
            Guard.ArgumentNotNull(userService, nameof(userService));
            Guard.ArgumentNotNull(fundingStreamPermissionService, nameof(fundingStreamPermissionService));

            _userService = userService;
            _fundingStreamPermissionService = fundingStreamPermissionService;
        }

        [Route("api/users/confirm-skills")]
        [HttpPost]
        public async Task<IActionResult> RunConfirmSkills()
        {
            return await _userService.ConfirmSkills(ControllerContext.HttpContext.Request);
        }

        [Route("api/users/get-user-by-userid")]
        [HttpGet]
        public async Task<IActionResult> RunGetUserByUserId()
        {
            return await _userService.GetUserByUserId(ControllerContext.HttpContext.Request);
        }

        [Route("api/users/{userId}/permissions")]
        [HttpGet]
        public async Task<IActionResult> RunGetFundingStreamPermissionsForUser([FromRoute]string userId)
        {
            return await _fundingStreamPermissionService.GetFundingStreamPermissionsForUser(userId, ControllerContext.HttpContext.Request);
        }

        [Route("api/users/{userId}/permissions/{fundingStreamId}")]
        [HttpPut]
        public async Task<IActionResult> RunUpdatePermissionForUser([FromRoute]string userId, [FromRoute]string fundingStreamId)
        {
            return await _fundingStreamPermissionService.UpdatePermissionForUser(userId, fundingStreamId, ControllerContext.HttpContext.Request);
        }

        [Route("api/users/{userid}/effectivepermissions/{specificationId}")]
        [HttpGet]
        public async Task<IActionResult> RunGetEffectivePermissionsForUser([FromRoute]string userId, [FromRoute]string specificationId)
        {
            return await _fundingStreamPermissionService.GetEffectivePermissionsForUser(userId, specificationId, ControllerContext.HttpContext.Request);
        }
    }
}