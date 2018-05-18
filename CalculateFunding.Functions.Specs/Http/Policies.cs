using System;
using System.Threading.Tasks;
using CalculateFunding.Services.Specs.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CalculateFunding.Functions.Specs.Http
{
    public static class Policies
    {
        [FunctionName("policy-create")]
        public static Task<IActionResult> RunCreatePolicy(
           [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req, ILogger log)
        {
            IServiceProvider provider = IocConfig.Build();

            ISpecificationsService svc = provider.GetService<ISpecificationsService>();

            return svc.CreatePolicy(req);
        }

        [FunctionName("policy-by-name")]
        public static Task<IActionResult> RunPolicyByName(
          [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req, ILogger log)
        {
            IServiceProvider provider = IocConfig.Build();

            ISpecificationsService svc = provider.GetService<ISpecificationsService>();

            return svc.GetPolicyByName(req);
        }

        [FunctionName("policy-edit")]
        public static Task<IActionResult> RunEditPolicy(
          [HttpTrigger(AuthorizationLevel.Function, "put")] HttpRequest req, ILogger log)
        {
            IServiceProvider provider = IocConfig.Build();

            ISpecificationsService svc = provider.GetService<ISpecificationsService>();

            return svc.EditPolicy(req);
        }
    }

}
