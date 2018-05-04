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
using CalculateFunding.Services.Core.Extensions;

namespace CalculateFunding.Functions.Specs.Http
{
    public static class Specifications
    {
        [FunctionName("specification-by-id")]
        public static Task<IActionResult> RunGetSpecificationById(
            [HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequest req, ILogger log)
        {
            using (var scope = IocConfig.Build().CreateScope())
            {
                ISpecificationsService svc = scope.ServiceProvider.GetService<ISpecificationsService>();
                return svc.GetSpecificationById(req);
            }
        }

        [FunctionName("specifications")]
        public static Task<IActionResult> RunGetSpecifications(
            [HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequest req, ILogger log)
        {
            using (var scope = IocConfig.Build().CreateScope())
            {
                ISpecificationsService svc = scope.ServiceProvider.GetService<ISpecificationsService>();
                return svc.GetSpecifications(req);
            }
        }
        [FunctionName("specifications-by-year")]
        public static Task<IActionResult> RunSpecificationsByYear(
            [HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequest req, ILogger log)
        {
            using (var scope = IocConfig.Build().CreateHttpScope(req))
            {
                return DoAsync(() =>
                {
                    ISpecificationsService svc = scope.ServiceProvider.GetService<ISpecificationsService>();
                    return svc.GetSpecificationByAcademicYearId(req);
                });
            }
        }

        [FunctionName("specification-by-name")]
        public static Task<IActionResult> RunSpecificationByName(
           [HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequest req, ILogger log)
        {
            using (var scope = IocConfig.Build().CreateScope())
            {
                ISpecificationsService svc = scope.ServiceProvider.GetService<ISpecificationsService>();

                return svc.GetSpecificationByName(req);
            }
        }

        [FunctionName("specification-create")]
        public static async Task<IActionResult> RunCreateSpecification(
            [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req, ILogger log)
        {
            using (var scope = IocConfig.Build().CreateScope())
            {
                ISpecificationsService svc = scope.ServiceProvider.GetService<ISpecificationsService>();

                return await svc.CreateSpecification(req);
            }
        }

        [FunctionName("specifications-search")]
        public static async Task<IActionResult> RunSearchSpecifications(
            [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req, ILogger log)
        {
            using (var scope = IocConfig.Build().CreateScope())
            {
                ISpecificationsSearchService svc = scope.ServiceProvider.GetService<ISpecificationsSearchService>();

                return await svc.SearchSpecifications(req);
            }
        }

        [FunctionName("reindex")]
        public static Task<IActionResult> RunReindex(
          [HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequest req, ILogger log)
        {
            using (var scope = IocConfig.Build().CreateScope())
            {
                ISpecificationsService svc = scope.ServiceProvider.GetService<ISpecificationsService>();

                return svc.ReIndex();
            }
        }

        async static public Task<T> DoAsync<T>(Func<Task<T>> func,  Func<T, Task> test = null)
        {
            try
            {
                var result = await func().ConfigureAwait(false);
                if (test != null)
                    await test.Invoke(result).ConfigureAwait(false);

                return result;
            }
            catch
            {
                throw new ApplicationException("Knifed");
            }
         }
    }
}
