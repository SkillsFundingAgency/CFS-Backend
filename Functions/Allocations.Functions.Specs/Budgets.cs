using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Allocations.Models.Specs;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Allocations.Repository;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Allocations.Functions.Specs
{
    public static class Budgets
    {
        private static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            Formatting = Formatting.Indented

        };

        [FunctionName("budgets")]
        public static async Task<HttpResponseMessage> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", "get")] HttpRequestMessage req, TraceWriter log)
        {
            string budgetId = req.GetQueryNameValuePairs()
                .FirstOrDefault(q => String.Compare(q.Key, "budgetId", StringComparison.OrdinalIgnoreCase) == 0)
                .Value;


            if (req.Method == HttpMethod.Post)
            {
                return await OnPost(req);
            }

            return await OnGet(budgetId);

        }

        private static async Task<HttpResponseMessage> OnGet(string budgetId)
        {
            using (var repository = new Repository<Budget>("specs"))
            {
                if (budgetId != null)
                {
                    var budget = await repository.ReadAsync(budgetId);
                    if (budget == null) return new HttpResponseMessage(HttpStatusCode.NotFound);
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(JsonConvert.SerializeObject(budget,
                            SerializerSettings), System.Text.Encoding.UTF8, "application/json")
                    };
                }

                var budgets = repository.Query().ToList();
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(JsonConvert.SerializeObject(budgets, SerializerSettings),
                        System.Text.Encoding.UTF8, "application/json")
                };
            }
        }

        private static async Task<HttpResponseMessage> OnPost(HttpRequestMessage req)
        {
            var json = await req.Content.ReadAsStringAsync();

            var budget = JsonConvert.DeserializeObject<Budget>(json, SerializerSettings);

            if (budget == null)
            {
                return req.CreateResponse(HttpStatusCode.BadRequest,
                    "Please ensure budget is passed in the request body");
            }

            using (var repository = new Repository<Budget>("specs"))
            {
                await repository.CreateAsync(budget);
            }

            return req.CreateResponse(HttpStatusCode.Created);
        }
    }

}
