using System;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Allocations.Models.Results;
using Allocations.Repository;
using Microsoft.Azure.Search;
using Microsoft.Azure.Search.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Allocations.Functions.Results
{
    public static class GetProductTestResults
    {
        private static readonly ISearchIndexClient SearchIndexClient;

        static GetProductTestResults()
        {
            var searchServiceClient = new SearchServiceClient(ConfigurationManager.AppSettings["SearchServiceName"], new SearchCredentials(ConfigurationManager.AppSettings["SearchServicePrimaryKey"]));
            SearchIndexClient = searchServiceClient.Indexes.GetClient((typeof(ProductTestScenarioResultIndex).Name.ToLowerInvariant()));
        }

        [FunctionName("GetProductTestResults")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)]HttpRequestMessage req, TraceWriter log)
        {
            log.Info("C# HTTP trigger function processed a request.");

            // parse query parameter
            string searchTerm = req.GetQueryNameValuePairs()
                .FirstOrDefault(q => String.Compare(q.Key, "searchTerm", StringComparison.OrdinalIgnoreCase) == 0)
                .Value;


            
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

            return req.CreateResponse(HttpStatusCode.OK, JsonConvert.SerializeObject(response, new JsonSerializerSettings{ ContractResolver = new CamelCasePropertyNamesContractResolver(), Formatting = Formatting.Indented}));
        }



    }
}
