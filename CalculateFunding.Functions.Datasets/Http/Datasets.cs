using System.Threading.Tasks;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Datasets.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace CalculateFunding.Functions.Datasets.Http
{
    public static class Datasets
    {
        [FunctionName("datasets-search")]
        public static Task<IActionResult> RunSearchDataDefinitions(
         [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req, ILogger log)
        {
            using (IServiceScope scope = IocConfig.Build().CreateHttpScope(req))
            {
                IDatasetSearchService svc = scope.ServiceProvider.GetService<IDatasetSearchService>();

                return svc.SearchDatasets(req);
            }
        }

        [FunctionName("create-new-dataset")]
        public static Task<IActionResult> RunCreateDataset(
         [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req, ILogger log)
        {
            using (var scope = IocConfig.Build().CreateHttpScope(req))
            {
                IDatasetService svc = scope.ServiceProvider.GetService<IDatasetService>();

                return svc.CreateNewDataset(req);
            }
        }

        [FunctionName("validate-dataset")]
        public static Task<IActionResult> RunValidateDataset(
        [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req, ILogger log)
        {
            using (var scope = IocConfig.Build().CreateHttpScope(req))
            {
                IDatasetService svc = scope.ServiceProvider.GetService<IDatasetService>();

                return svc.ValidateDataset(req);
            }
        }
    }
}
