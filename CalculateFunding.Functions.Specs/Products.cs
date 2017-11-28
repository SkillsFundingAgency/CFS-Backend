using Allocations.Repository;
using CalculateFunding.Models;
using CalculateFunding.Models.Specs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;

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
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", "get")] HttpRequest req, TraceWriter log)
        {
            req.Query.TryGetValue("budgetId", out var budgetId);
            req.Query.TryGetValue("productId", out var productId);

            if (req.Method == "POST")
            {
                return await OnPost(req, budgetId);
            }


            if (budgetId.FirstOrDefault() == null)
            {
                return new BadRequestErrorMessageResult("budgetId is required");
            }

            return await OnGet(budgetId, productId);

        }

        private static async Task<IActionResult> OnGet(string budgetId, string productId)
        {

            using (var repository = new Repository<Budget>("specs"))
            {

                var budget = await repository.ReadAsync(budgetId);
                var product = budget?.GetProduct(productId);
                if (product == null) return new NotFoundResult();

                return new JsonResult(product);
            }
        }

        private static async Task<IActionResult> OnPost(HttpRequest req, string budgetId)
        {
            var json = await req.ReadAsStringAsync();

            var product = JsonConvert.DeserializeObject<Product>(json, SerializerSettings);

            if (product == null)
            {
                return new BadRequestErrorMessageResult("Please ensure product is passed in the request body");
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

            return new AcceptedResult();
        }
    }

}
