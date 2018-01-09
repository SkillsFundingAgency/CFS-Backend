using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using CalculateFunding.Models;
using CalculateFunding.Repositories.Common.Cosmos;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
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
        public async Task<IActionResult> Run(HttpRequest req, ILogger log, string idName)
        {
            req.Query.TryGetValue(idName, out var id);

            return await OnGet(id.FirstOrDefault());

        }

        public async Task<IActionResult> Run(ILogger log, Expression<Func<T, bool>> query)
        {
            return await OnGetQueryAsync(query);
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

        private async Task<IActionResult> OnGetQueryAsync(Expression<Func<T, bool>> query)
        {
            var repository = ServiceFactory.GetService<CosmosRepository>();

            var entities = repository.Query<T>().Where(query).ToList();
            return new JsonResult(entities, SerializerSettings);
        }

    }
}