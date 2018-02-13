using System.Net.Http;
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
    public static class Datasets
    {
        [FunctionName("datasets-search")]
        public static Task<IActionResult> RunSearchDataDefinitions(
         [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
        {
            using (IServiceScope scope = IocConfig.Build().CreateHttpScope(req))
            {
                IDatasetSearchService svc = scope.ServiceProvider.GetService<IDatasetSearchService>();

                return svc.SearchDatasets(req);
            }
        }

        [FunctionName("create-new-dataset")]
        public static Task<IActionResult> RunCreateDataset(
         [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
        {
            using (var scope = IocConfig.Build().CreateHttpScope(req))
            {
                IDatasetService svc = scope.ServiceProvider.GetService<IDatasetService>();

                return svc.CreateNewDataset(req);
            }
        }

        [FunctionName("validate-dataset")]
        public static Task<IActionResult> RunValidateDataset(
        [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
        {
            using (var scope = IocConfig.Build().CreateHttpScope(req))
            {
                IDatasetService svc = scope.ServiceProvider.GetService<IDatasetService>();

                return svc.ValidateDataset(req);
            }
        }

        [FunctionName("test-http-client")]
        public static IActionResult Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)]HttpRequest req)
        {
            string name = req.Query["name"];

            HttpClient client = new HttpClient();
            string responseMessage = "Initial";

            string url = "https://www.google.com/";

            try
            {
                HttpResponseMessage response = client.GetAsync(url).Result;
                if (response == null)
                {
                    responseMessage = "Response was null";
                }
                else
                {
                    responseMessage = $"Response returned {response.StatusCode} for URL: {url}<pre>{response.Content.ReadAsStringAsync().Result}</pre>";
                }
            }
            catch (System.Exception ex)
            {

                responseMessage = $"Exception thrown {ex} with message {ex.Message}";
            }

            return new OkObjectResult(responseMessage);
        }
    }
}
