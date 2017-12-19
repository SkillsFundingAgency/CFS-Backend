using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Models;
using CalculateFunding.Repositories.Common.Cosmos;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace CalculateFunding.Functions.Common
{
    public class RestGetMethods<T> where T : IIdentifiable
    {
        private static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            Formatting = Formatting.Indented

        };
        public async Task<IActionResult> Run(HttpRequest req, TraceWriter log, string idName)
        {
            req.Query.TryGetValue(idName, out var id);

            return await OnGet(id.FirstOrDefault());

        }

        private async Task<IActionResult> OnGet(string id)
        {
            var repository = ServiceFactory.GetService<CosmosRepository>();

            if (id != null)
            {
                var entity = await repository.ReadAsync<T>(id);
                if (entity == null) return new NotFoundResult();
                return new JsonResult(entity.Content, SerializerSettings);

            }

            var entities = repository.Query<T>().ToList();
            return new JsonResult(entities, SerializerSettings);

        }

    }
}