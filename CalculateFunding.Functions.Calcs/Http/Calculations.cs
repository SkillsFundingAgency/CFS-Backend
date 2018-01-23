using System.Threading.Tasks;
using CalculateFunding.Functions.Common;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Specs;
using CalculateFunding.Services.Calcs.Interfaces;
using CalculateFunding.Services.Core.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CalculateFunding.Functions.Calcs.Http
{
    public static class Calculations
    {
        //[FunctionName("calculations")]
        //public static async Task<IActionResult> Run(
        //    [HttpTrigger(AuthorizationLevel.Function, "post", "get")] HttpRequest req, ILogger log)
        //{
        //    var restMethods = new RestGetMethods<Calculation>();
        //    return await restMethods.Run(req, log, "specificationId");
        //}

        [FunctionName("calculations-search")]
        public static Task<IActionResult> RunCalculationsSearch(
           [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req, ILogger log)
        {
            using (var scope = IocConfig.Build().CreateHttpScope(req))
            {
                ICalculationService svc = scope.ServiceProvider.GetService<ICalculationService>();
                return svc.SearchCalculations(req);
            }
        }

        [FunctionName("calculation-by-id")]
        public static Task<IActionResult> RunCalculationById(
         [HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequest req, ILogger log)
        {
            using (var scope = IocConfig.Build().CreateHttpScope(req))
            {
                ICalculationService svc = scope.ServiceProvider.GetService<ICalculationService>();

                return svc.GetCalculationById(req);
            }
        }
    }
}
