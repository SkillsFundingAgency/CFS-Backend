using System.Threading.Tasks;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Datasets.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.DependencyInjection;


namespace CalculateFunding.Functions.Datasets.Http
{
    public static class DataDefinitions
    {
        [FunctionName("data-definitions")]
        public static Task<IActionResult> RunDataDefinitions(
             [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
        {
            using (var scope = IocConfig.Build().CreateHttpScope(req))
            {
                IDefinitionsService svc = scope.ServiceProvider.GetService<IDefinitionsService>();

                return svc.SaveDefinition(req);
            }
        }

        [FunctionName("data-definitions")]
        public static Task<IActionResult> RunGetDataDefinitions(
         [HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequest req)
        {
            using (var scope = IocConfig.Build().CreateHttpScope(req))
            {
                IDefinitionsService svc = scope.ServiceProvider.GetService<IDefinitionsService>();

                return svc.GetDatasetDefinitions(req);
            }
        }
    }
}
