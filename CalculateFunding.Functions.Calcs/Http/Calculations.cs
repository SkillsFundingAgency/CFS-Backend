using System.Threading.Tasks;
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
        [FunctionName("calculations-search")]
        public static Task<IActionResult> RunCalculationsSearch(
           [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req, ILogger log)
        {
            using (var scope = IocConfig.Build().CreateHttpScope(req))
            {
                ICalculationsSearchService svc = scope.ServiceProvider.GetService<ICalculationsSearchService>();
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

        [FunctionName("calculation-publish")]
        public static Task<IActionResult> RunCalculationPublish(
         [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req, ILogger log)
        {
            using (var scope = IocConfig.Build().CreateHttpScope(req))
            {
                ICalculationService svc = scope.ServiceProvider.GetService<ICalculationService>();

                return svc.PublishCalculationVersion(req);
            }
        }

        [FunctionName("calculation-current-version")]
        public static Task<IActionResult> RunCalculationCurrentVersion(
        [HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequest req, ILogger log)
        {
            using (var scope = IocConfig.Build().CreateHttpScope(req))
            {
                ICalculationService svc = scope.ServiceProvider.GetService<ICalculationService>();

                return svc.GetCalculationCurrentVersion(req);
            }
        }

        [FunctionName("calculation-version-history")]
        public static Task<IActionResult> RunCalculationVersions(
         [HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequest req, ILogger log)
        {
            using (var scope = IocConfig.Build().CreateHttpScope(req))
            {
                ICalculationService svc = scope.ServiceProvider.GetService<ICalculationService>();

                return svc.GetCalculationHistory(req);
            }
        }

        [FunctionName("calculation-versions")]
        public static Task<IActionResult> RunCalculationCompareVersions(
         [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req, ILogger log)
        {
            using (var scope = IocConfig.Build().CreateHttpScope(req))
            {
                ICalculationService svc = scope.ServiceProvider.GetService<ICalculationService>();

                return svc.GetCalculationVersions(req);
            }
        }

        [FunctionName("calculation-save-version")]
        public static Task<IActionResult> RunCalculationSaveVersion(
        [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req, ILogger log)
        {
            using (var scope = IocConfig.Build().CreateHttpScope(req))
            {
                ICalculationService svc = scope.ServiceProvider.GetService<ICalculationService>();

                return svc.SaveCalculationVersion(req);
            }
        }

        [FunctionName("get-calculation-code-context")]
        public static Task<IActionResult> RunGetCalculationCodeContext(
        [HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequest req, ILogger log)
        {
            using (var scope = IocConfig.Build().CreateHttpScope(req))
            {
                ICalculationService svc = scope.ServiceProvider.GetService<ICalculationService>();

                return svc.GetCalculationCodeContext(req);
            }
        }

        [FunctionName("get-buildproject-by-specification-id")]
        public static Task<IActionResult> RunGetBuildProjectBySpecificationId(
        [HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequest req, ILogger log)
        {
            using (var scope = IocConfig.Build().CreateHttpScope(req))
            {
                IBuildProjectsService svc = scope.ServiceProvider.GetService<IBuildProjectsService>();

                return svc.GetBuildProjectBySpecificationId(req);
            }
        }

        [FunctionName("update-buildproject-relationships")]
        public static Task<IActionResult> RunUpdateBuildProjectRealtionships(
       [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req, ILogger log)
        {
            using (var scope = IocConfig.Build().CreateHttpScope(req))
            {
                IBuildProjectsService svc = scope.ServiceProvider.GetService<IBuildProjectsService>();

                return svc.UpdateBuildProjectRelationships(req);
            }
        }
    }
}
