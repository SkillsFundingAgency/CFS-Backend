using System.Threading.Tasks;
using CalculateFunding.Functions.Common;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Repositories.Common.Search;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace CalculateFunding.Functions.Datasets.Http
{
    public static class Providers
    {

        [FunctionName("providers")]
        public static async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)]HttpRequest req, TraceWriter log)
        {
            req.Query.TryGetValue("searchTerm", out var searchTerm);

            var searchRepository = ServiceFactory.GetService<SearchRepository<ProviderIndex>>();

            var searchResults = await searchRepository.Search(searchTerm);

            return new JsonResult(searchResults);
        }

        [FunctionName("providers-commands")]
        public static async Task<IActionResult> RunCommands(
            [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req, ILogger log)
        {
            var restMethods = new RestCommandMethods<Provider, ProviderCommand>("dataset-events");
            return await restMethods.Run(req, log);
        }

    }
}
