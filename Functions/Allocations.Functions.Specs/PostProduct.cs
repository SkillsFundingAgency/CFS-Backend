using System.Configuration;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Allocations.Models.Specs;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Allocations.Repository;

namespace Allocations.Functions.Specs
{
    public static class PostProduct
    {
        [FunctionName("PostProduct")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Function, "post", "put")] HttpRequestMessage req, string budgetId, TraceWriter log)
        {
            var product = await req.Content.ReadAsAsync<Product>();

            if (product == null)
            {
                return req.CreateResponse(HttpStatusCode.BadRequest,
                    "Please ensure product is passed in the request body");
            }

            using (var repository = new Repository<Budget>("specs"))
            {
                var budget = await repository.ReadAsync(budgetId);
                await repository.CreateAsync(budget);
            }

            return req.CreateResponse(HttpStatusCode.Created);
        }
    }
}
