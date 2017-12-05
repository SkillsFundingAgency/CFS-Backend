using System;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Models.Results;
using CalculateFunding.Repository;
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
        private static readonly ISearchIndexClient SearchIndexClient;

        static GetProductTestResults()
        {
            var searchServiceClient = new SearchServiceClient(Environment.GetEnvironmentVariable("SearchServiceName"), new SearchCredentials(Environment.GetEnvironmentVariable("SearchServicePrimaryKey")));
            SearchIndexClient = searchServiceClient.Indexes.GetClient((typeof(ProductTestScenarioResultIndex).Name.ToLowerInvariant()));
        }

        [FunctionName("product-tests")]
        public static async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)]HttpRequest req, TraceWriter log)
        {
            log.Info("C# HTTP trigger function processed a request.");
            req.Query.TryGetValue("searchTerm", out var searchTerm);

            var azureSearchResult = await SearchIndexClient.Documents.SearchAsync<ProductTestScenarioResultIndex>(searchTerm, new SearchParameters { IncludeTotalResultCount = true });

            var response = new SearchResults<ProductTestScenarioResultIndex>
            {
                SearchTerm = searchTerm,
                TotalCount = azureSearchResult.Count,

                Results = azureSearchResult.Results.Select(x => new Repository.SearchResult<ProductTestScenarioResultIndex>
                {
                    HitHighLights = x.Highlights,
                    Result = x.Document,
                    Score = x.Score
                }).ToArray()
            };

            return new OkObjectResult(JsonConvert.SerializeObject(response,
                new JsonSerializerSettings
                {
                    ContractResolver = new CamelCasePropertyNamesContractResolver(),
                    Formatting = Formatting.Indented
                }));
        }



    }
}
