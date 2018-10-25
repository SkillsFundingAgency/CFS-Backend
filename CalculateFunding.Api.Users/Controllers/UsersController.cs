using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Users.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace CalculateFunding.Api.Users.Controllers
{
    public class UsersController : Controller
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            Guard.ArgumentNotNull(userService, nameof(userService));

            _userService = userService;
        }

        [Route("api/users/confirm-skills")]
        [HttpPost]
        public Task<IActionResult> RunConfirmSkills()
        {
            return _userService.ConfirmSkills(ControllerContext.HttpContext.Request);
        }

        [Route("api/users/get-user-by-userid")]
        [HttpGet]
        public Task<IActionResult> RunGetUserByUserId()
        {
            return _userService.GetUserByUserId(ControllerContext.HttpContext.Request);
        }
    }
}