using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using CalculateFunding.Models;
using CalculateFunding.Repositories.Common.Cosmos;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace CalculateFunding.Functions.Common
{
    public static class RestMethods<T> where T : Reference
    {
        private static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            Formatting = Formatting.Indented

        };
        public static async Task<IActionResult> Run(HttpRequest req, TraceWriter log, string idName)
        {
            req.Query.TryGetValue(idName, out var id);


            if (req.Method == "POST")
            {
                return await OnPost(req);
            }

            return await OnGet(id.FirstOrDefault());

        }

        private static async Task<IActionResult> OnGet(string id)
        {
            var repository = ServiceFactory.GetService<CosmosRepository<T>>();

            if (id != null)
            {
                var entity = await repository.ReadAsync(id);
                if (entity == null) return new NotFoundResult();
                return new JsonResult(entity.Content, SerializerSettings);

            }

            var entities = repository.Query().ToList();
            return new JsonResult(entities, SerializerSettings);

        }

        private static async Task<IActionResult> OnPost(HttpRequest req)
        {
            var json = await req.ReadAsStringAsync();

            var item = JsonConvert.DeserializeObject<T>(json, SerializerSettings);

            if (item == null)
            {
                return new BadRequestErrorMessageResult("Please ensure entity is passed in the request body");
            }

            var repository = ServiceFactory.GetService<CosmosRepository<T>>();
            await repository.EnsureCollectionExists();
            var current = await repository.ReadAsync(item.Id);
            if (current.Content != null)
            {
                if (!IsModified(current.Content, item))
                {
                    return new StatusCodeResult(304);
                }
            }


            await repository.CreateAsync(item);

            return new AcceptedResult();
        }

        private static bool IsModified<TAny>(TAny current, TAny item)
        {
            return JsonConvert.SerializeObject(current) == JsonConvert.SerializeObject(item);
        }


        private static async Task<IActionResult> OnPost(string json)
        {
            var item = JsonConvert.DeserializeObject<T>(json, SerializerSettings);

            if (item == null)
            {
                return new BadRequestErrorMessageResult("Please ensure entity is passed in the request body");
            }

            // get current
            // check updated
            // update
            // add event

            var repository = ServiceFactory.GetService<CosmosRepository<T>>();
            await repository.EnsureCollectionExists();
            await repository.CreateAsync(item);

            return new AcceptedResult();
        }


    }
}