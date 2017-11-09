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
            [HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequestMessage req, TraceWriter log)
        {
            string budgetId = req.GetQueryNameValuePairs()
                .FirstOrDefault(q => String.Compare(q.Key, "budgetId", StringComparison.OrdinalIgnoreCase) == 0)
                .Value;


            if (budgetId == null)
            {
            }

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
    }

}
