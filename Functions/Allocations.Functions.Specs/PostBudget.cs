using System;
using System.Configuration;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Allocations.Models.Budgets;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Allocations.Repository;

namespace Allocations.Functions.Specs
{
    public static class PostBudget
    {
        [FunctionName("PostBudget")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Function, "post")]HttpRequestMessage req, TraceWriter log)
        {
            var budget = await req.Content.ReadAsAsync<Budget>();

            if (budget == null)
            {
                return req.CreateResponse(HttpStatusCode.BadRequest, "Please ensure budget is passed in the request body");
            }

            var databaseName = ConfigurationManager.AppSettings["DocumentDB.DatabaseName"];
            var endpoint = new Uri(ConfigurationManager.AppSettings["DocumentDB.Endpoint"]);
            var key = ConfigurationManager.AppSettings["DocumentDB.Key"];
            using (var repository = new Repository<Budget>("definitions"))
            {
                await repository.CreateAsync(budget);
            }

            return req.CreateResponse(HttpStatusCode.Created);
        }
    }
}
