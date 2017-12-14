
using System.IO;
using System.Threading.Tasks;
using CalculateFunding.Functions.Common;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Repositories.Common.Search;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;

namespace CalculateFunding.Functions.Providers
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

    }
}
