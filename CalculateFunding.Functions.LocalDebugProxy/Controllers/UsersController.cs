using CalculateFunding.Services.Users.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CalculateFunding.Functions.LocalDebugProxy.Controllers
{
    public class UsersController : BaseController
    {
        private readonly IUserService _userService;

        public UsersController(IServiceProvider serviceProvider, IUserService userService)
            : base(serviceProvider)
        {
            _userService = userService;
        }

        [Route("api/users/confirm-skills")]
        [HttpPost]
        public Task<IActionResult> RunConfirmSkills()
        {
            SetUserAndCorrelationId(ControllerContext.HttpContext.Request);

            return _userService.ConfirmSkills(ControllerContext.HttpContext.Request);
        }

        [Route("api/users/get-user-by-username")]
        [HttpGet]
        public Task<IActionResult> RunGetUserByUsername()
        {
            SetUserAndCorrelationId(ControllerContext.HttpContext.Request);

            return _userService.GetUserByUsername(ControllerContext.HttpContext.Request);
        }
    }
}
