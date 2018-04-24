using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Scenarios.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;


namespace CalculateFunding.Functions.Scenarios.Http
{
    public static class Scenarios
    {
        [FunctionName("save-scenario-test-version")]
        public static Task<IActionResult> RunSaveScenarioTestVersion(
           [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
        {
            using (var scope = IocConfig.Build().CreateHttpScope(req))
            {
                IScenariosService svc = scope.ServiceProvider.GetService<IScenariosService>();
                return svc.SaveVersion(req);
            }
        }

        [FunctionName("scenarios-search")]
        public static Task<IActionResult> RunScenariosSearch(
          [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
        {
            using (var scope = IocConfig.Build().CreateHttpScope(req))
            {
                IScenariosSearchService svc = scope.ServiceProvider.GetService<IScenariosSearchService>();
                return svc.SearchScenarios(req);
            }
        }

        [FunctionName("get-scenarios-by-specificationId")]
        public static Task<IActionResult>RunGetTestScenariosBySpecificationId(
         [HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequest req)
        {
            using (var scope = IocConfig.Build().CreateHttpScope(req))
            {
                IScenariosService svc = scope.ServiceProvider.GetService<IScenariosService>();
                return svc.GetTestScenariosBySpecificationId(req);
            }
        }

        [FunctionName("get-scenario-by-id")]
        public static Task<IActionResult> RunGetTestScenarioById(
        [HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequest req)
        {
            using (var scope = IocConfig.Build().CreateHttpScope(req))
            {
                IScenariosService svc = scope.ServiceProvider.GetService<IScenariosService>();
                return svc.GetTestScenarioById(req);
            }
        }

        [FunctionName("scenarios-search-reindex")]
        public static Task<IActionResult> RunScenariosSearchReIndex(
          [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
        {
            using (var scope = IocConfig.Build().CreateHttpScope(req))
            {
                IScenariosSearchService svc = scope.ServiceProvider.GetService<IScenariosSearchService>();
                return svc.ReIndex(req);
            }
        }
    }
}
