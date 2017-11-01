using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Allocations.Models.Results;
using Allocations.Repository;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;

namespace Allocations.Functions.Results
{
    public static class GetBudgets
    {
        [FunctionName("GetBudgets")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Function, "get", Route = null)]HttpRequestMessage req, TraceWriter log)
        {
            using (var resultsRepository = new Repository<ProviderResult>("results"))
            {
                var providerResults = resultsRepository.Query().ToList();
                var results = providerResults.GroupBy(x => x.Budget.Id).Select(x => new
                {
                    x.First().Budget,
                    total = x.Sum(p => p.ProductResults?.Sum(pr => pr.Value))
            });

                return req.CreateResponse(HttpStatusCode.OK, JsonConvert.SerializeObject(results, Formatting.Indented));
            }

        }

    }
}
