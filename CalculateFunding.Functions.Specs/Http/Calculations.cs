using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using CalculateFunding.Functions.Common;
using CalculateFunding.Models.Specs;
using CalculateFunding.Repositories.Common.Cosmos;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Specs.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace CalculateFunding.Functions.Specs.Http
{
    public static class Calculations
    {
        [FunctionName("calculation-create")]
        public static Task<IActionResult> RunCreateCalculation(
           [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req, ILogger log)
        {
            using (var scope = IocConfig.Build().CreateHttpScope(req))
            {
                ISpecificationsService svc = scope.ServiceProvider.GetService<ISpecificationsService>();

                return svc.CreateCalculation(req);
            }
        }

        [FunctionName("calculation-by-name")]
        public static Task<IActionResult> RunCalculationByName(
          [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req, ILogger log)
        {
            using (var scope = IocConfig.Build().CreateHttpScope(req))
            {
                ISpecificationsService svc = scope.ServiceProvider.GetService<ISpecificationsService>();

                return svc.GetCalculationByName(req);
            }
        }

        [FunctionName("calculation-by-id")]
        public static Task<IActionResult> RunCalculationBySpecificationIdAndCalculationId(
         [HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequest req, ILogger log)
        {
            using (var scope = IocConfig.Build().CreateHttpScope(req))
            {
                ISpecificationsService svc = scope.ServiceProvider.GetService<ISpecificationsService>();

                return svc.GetCalculationBySpecificationIdAndCalculationId(req);
            }
        }

        [FunctionName("allocation-lines")]
        public static Task<IActionResult> RunAllocationLines(
         [HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequest req, ILogger log)
        {
            using (var scope = IocConfig.Build().CreateHttpScope(req))
            {
                ISpecificationsService svc = scope.ServiceProvider.GetService<ISpecificationsService>();

                return svc.GetAllocationLines(req);
            }
        }

    }

}
