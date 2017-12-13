using System;
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

namespace CalculateFunding.Functions.Specs.Http
{
    public static class Specifications
    {
        private static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            Formatting = Formatting.Indented

        };

        [FunctionName("specifications")]
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
            var repository = GetRepository();
            
            if (budgetId != null)
            {
                var budget = await repository.ReadAsync(budgetId);
                if (budget == null) return new NotFoundResult();
                return new OkObjectResult(JsonConvert.SerializeObject(budget, SerializerSettings));

            }

            var budgets = repository.Query().ToList();
            return new OkObjectResult(JsonConvert.SerializeObject(budgets, SerializerSettings));
            
        }

        private static async Task<IActionResult> OnPost(HttpRequest req)
        {
            var json = await req.ReadAsStringAsync();

            var budget = JsonConvert.DeserializeObject<Budget>(json, SerializerSettings);

            if (budget == null)
            {
                return new BadRequestErrorMessageResult("Please ensure budget is passed in the request body");
            }

            var repository = GetRepository();
            await repository.CreateAsync(budget);

            return new AcceptedResult();
        }

        private static Repository<Budget> GetRepository()
        {
            var repository = new Repository<Budget>(new RepositorySettings
            {
                ConnectionString = Environment.GetEnvironmentVariable(""),
                CollectionName = "specs",
                DatabaseName = "calculate-funding"
            }, null);
            return repository;
        }
    }

}
