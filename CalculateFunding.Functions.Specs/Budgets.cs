using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using CalculateFunding.Models.Specs;
using CalculateFunding.Repository;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace CalculateFunding.Functions.Specs
{
    public static class Budgets
    {
        private static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            Formatting = Formatting.Indented

        };

        [FunctionName("budgets")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", "get")] HttpRequest req, TraceWriter log)
        {
            req.Query.TryGetValue("budgetId", out var budgetId);


            if (req.Method == "POST")
            {
                return await OnPost(req);
            }

            return await OnGet(budgetId.FirstOrDefault());

        }

        private static async Task<IActionResult> OnGet(string budgetId)
        {
            using (var repository = new Repository<Budget>("specs"))
            {
                if (budgetId != null)
                {
                    var budget = await repository.ReadAsync(budgetId);
                    if (budget == null) return new NotFoundResult();
                    return new OkObjectResult(JsonConvert.SerializeObject(budget, SerializerSettings));

                }

                var budgets = repository.Query().ToList();
                return new OkObjectResult(JsonConvert.SerializeObject(budgets, SerializerSettings));
            }
        }

        private static async Task<IActionResult> OnPost(HttpRequest req)
        {
            var json = await req.ReadAsStringAsync();

            var budget = JsonConvert.DeserializeObject<Budget>(json, SerializerSettings);

            if (budget == null)
            {
                return new BadRequestErrorMessageResult("Please ensure budget is passed in the request body");
            }

            using (var repository = new Repository<Budget>("specs"))
            {
                await repository.CreateAsync(budget);
            }

            return new AcceptedResult();
        }
    }

}
