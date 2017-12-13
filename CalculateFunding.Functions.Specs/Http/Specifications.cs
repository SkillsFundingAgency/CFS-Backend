using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using CalculateFunding.Functions.Common;
using CalculateFunding.Models.Specs;
using CalculateFunding.Repositories.Common.Cosmos;
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
            req.Query.TryGetValue("specificationId", out var specificationId);


            if (req.Method == "POST")
            {
                return await OnPost(req);
            }

            return await OnGet(specificationId.FirstOrDefault());

        }

        private static async Task<IActionResult> OnGet(string specificationId)
        {
            var repository = ServiceFactory.GetService<Repository<Specification>>();

            if (specificationId != null)
            {
                var budget = await repository.ReadAsync(specificationId);
                if (budget == null) return new NotFoundResult();
                return new OkObjectResult(JsonConvert.SerializeObject(budget, SerializerSettings));

            }

            var budgets = repository.Query().ToList();
            return new OkObjectResult(JsonConvert.SerializeObject(budgets, SerializerSettings));
            
        }

        private static async Task<IActionResult> OnPost(HttpRequest req)
        {
            var json = await req.ReadAsStringAsync();

            var item = JsonConvert.DeserializeObject<Specification>(json, SerializerSettings);

            if (item == null)
            {
                return new BadRequestErrorMessageResult("Please ensure budget is passed in the request body");
            }

            var repository = ServiceFactory.GetService<Repository<Specification>>();
            await repository.EnsureCollectionExists();
            await repository.CreateAsync(item);

            return new AcceptedResult();
        }
    }

}
