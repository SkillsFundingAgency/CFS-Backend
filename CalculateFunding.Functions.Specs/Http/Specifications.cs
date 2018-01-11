using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using CalculateFunding.Services.Specs.Interfaces;

namespace CalculateFunding.Functions.Specs.Http
{
    public static class Specifications
    {
        [FunctionName("specifications")]
        public static Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequest req, ILogger log)
        {
            IServiceProvider provider = IocConfig.Build();

            ISpecificationsService svc = provider.GetService<ISpecificationsService>();
            return svc.GetSpecificationById(req);
        }

        [FunctionName("specifications-by-year")]
        public static Task<IActionResult> RunSpecificationsByYear(
            [HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequest req, ILogger log)
        {
            IServiceProvider provider = IocConfig.Build();

            ISpecificationsService svc = provider.GetService<ISpecificationsService>();

            return svc.GetSpecificationByAcademicYearId(req);
        }

        [FunctionName("specification-by-name")]
        public static Task<IActionResult> RunSpecificationByName(
           [HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequest req, ILogger log)
        {
            IServiceProvider provider = IocConfig.Build();

            ISpecificationsService svc = provider.GetService<ISpecificationsService>();

            return svc.GetSpecificationByName(req);
        }

        [FunctionName("specification-create")]
        public static async Task<IActionResult> RunCreateSpecification(
            [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req, ILogger log)
        {
            IServiceProvider provider = IocConfig.Build();

            ISpecificationsService svc = provider.GetService<ISpecificationsService>();

            return await svc.CreateSpecification(req);
        }
    }
}
