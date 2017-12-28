using System;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Functions.Common;
using CalculateFunding.Models.Results;
using CalculateFunding.Repositories.Common.Search;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Search;
using Microsoft.Azure.Search.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace CalculateFunding.Functions.Results
{
    public static class GetProductTestResults
    {
        [FunctionName("product-tests")]
        public static async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)]HttpRequest req, TraceWriter log)
        {
            log.Info("C# HTTP trigger function processed a request.");
            req.Query.TryGetValue("searchTerm", out var searchTerm);

            var searchRepository = ServiceFactory.GetService<SearchRepository<ProductTestScenarioResultIndex>>();

            var searchResults = await searchRepository.Search(searchTerm);


            return new OkObjectResult(JsonConvert.SerializeObject(searchResults,
                new JsonSerializerSettings
                {
                    ContractResolver = new CamelCasePropertyNamesContractResolver(),
                    Formatting = Formatting.Indented
                }));
        }



    }
}
