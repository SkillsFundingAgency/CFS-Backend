using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Allocations.Models;
using Allocations.Models.Specs;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Allocations.Repository;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Allocations.Functions.Specs
{
    public static class Products
    {
        private static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            Formatting = Formatting.Indented

        };

        [FunctionName("products")]
        public static async Task<HttpResponseMessage> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", "get")] HttpRequestMessage req, TraceWriter log)
        {
            string budgetId = req.GetQueryNameValuePairs()
                .FirstOrDefault(q => String.Compare(q.Key, "budgetId", StringComparison.OrdinalIgnoreCase) == 0)
                .Value;

            string productId = req.GetQueryNameValuePairs()
                .FirstOrDefault(q => String.Compare(q.Key, "productId", StringComparison.OrdinalIgnoreCase) == 0)
                .Value;



            if (req.Method == HttpMethod.Post)
            {
                return await OnPost(req, budgetId);
            }


            if (budgetId == null)
            {
                return req.CreateResponse(HttpStatusCode.BadRequest, "budgetId is required");
            }

            return await OnGet(budgetId, productId);

        }

        private static async Task<HttpResponseMessage> OnGet(string budgetId, string productId)
        {

            using (var repository = new Repository<Budget>("specs"))
            {

                var budget = await repository.ReadAsync(budgetId);
                var product = budget?.GetProduct(productId);
                if (product == null) return new HttpResponseMessage(HttpStatusCode.NotFound);


                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(JsonConvert.SerializeObject(product,
                        SerializerSettings), System.Text.Encoding.UTF8, "application/json")
                };

            }
        }

        private static async Task<HttpResponseMessage> OnPost(HttpRequestMessage req, string budgetId)
        {
            var json = await req.Content.ReadAsStringAsync();

            var product = JsonConvert.DeserializeObject<Product>(json, SerializerSettings);

            if (product == null)
            {
                return req.CreateResponse(HttpStatusCode.BadRequest, "Please ensure product is passed in the request body");
            }

            using (var repository = new Repository<Budget>("specs"))
            {
                var budget = await repository.ReadAsync(budgetId);

                var existing = budget.GetProduct(product.Id);
                if (existing != null)
                {
                    existing.Name = product.Name;
                    existing.Description = product.Description;
                    existing.TestScenarios = product.TestScenarios;
                    existing.Calculation = product.Calculation;
                    existing.TestProviders = product.TestProviders;
                }
                else
                {
                    // TODO create
                }

                await repository.UpsertAsync(budget);
            }

            return req.CreateResponse(HttpStatusCode.Created);
        }
    }

}
