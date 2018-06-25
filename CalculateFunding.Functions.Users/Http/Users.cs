
using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using CalculateFunding.Services.Users.Interfaces;
using System.Threading.Tasks;
using Serilog;
using CalculateFunding.Services.Core.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace CalculateFunding.Functions.Users.Http
{
    public static class Users
    {
        [FunctionName("confirm-skills")]
        public static Task<IActionResult> RunConfirmSkills(
          [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req, ILogger log)
        {
            using (var scope = IocConfig.Build().CreateHttpScope(req))
            {
                IUserService svc = scope.ServiceProvider.GetService<IUserService>();

                return svc.ConfirmSkills(req);
            }
        }

        [FunctionName("get-user-by-username")]
        public static Task<IActionResult> RunGetUserByUsername(
         [HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequest req, ILogger log)
        {
            using (var scope = IocConfig.Build().CreateHttpScope(req))
            {
                IUserService svc = scope.ServiceProvider.GetService<IUserService>();

                return svc.GetUserByUsername(req);
            }
        }
    }
}
