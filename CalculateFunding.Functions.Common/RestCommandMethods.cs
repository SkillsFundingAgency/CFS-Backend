using System.Threading.Tasks;
using System.Web.Http;
using CalculateFunding.Models;
using CalculateFunding.Models.Specs;
using CalculateFunding.Repositories.Common.Cosmos;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace CalculateFunding.Functions.Common
{
    public class RestCommandMethods<T, TCommand> where T : IIdentifiable where TCommand : Command<T>
    {
        private static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            Formatting = Formatting.Indented

        };
        public async Task<IActionResult> Run(HttpRequest req, TraceWriter log)
        {
            var json = await req.ReadAsStringAsync();
            var command = JsonConvert.DeserializeObject<TCommand>(json, SerializerSettings);

            if (command == null)
            {
                return new BadRequestErrorMessageResult("Please ensure command is passed in the request body");
            }

            switch (command.Method?.ToLowerInvariant())
            {
                case "post":
                case "put":
                    return await OnPost(command);
                case "delete":
                    return await OnDelete(command);
                default:
                    return new BadRequestErrorMessageResult($"{command.Method} is not a supported method");

            }

        }

        private async Task<IActionResult> OnPost(TCommand command)
        {
            var repository = ServiceFactory.GetService<CosmosRepository>();
            var messenger = ServiceFactory.GetService<Messenger>();

            await repository.EnsureCollectionExists();
            var current = await repository.ReadAsync<T>(command.Content.Id);
            if (current.Content != null)
            {
                if (!IsModified(current.Content, command.Content))
                {
                    return new StatusCodeResult(304);
                }
            }
            await repository.CreateAsync(command.Content);
            await repository.CreateAsync(command);
            await messenger.SendAsync("spec-events", command);
            // send SB message

            return new AcceptedResult();
        }



        private async Task<IActionResult> OnDelete(TCommand command)
        {
            var repository = ServiceFactory.GetService<CosmosRepository>();
            var messenger = ServiceFactory.GetService<Messenger>();
            await repository.EnsureCollectionExists();
            var current = await repository.ReadAsync<T>(command.Content.Id);
            if (current.Content != null)
            {
                if (current.Deleted)
                { 
                    return new StatusCodeResult(304);
                }
            }
            current.Deleted = true;
            await repository.CreateAsync(current.Content);
            await repository.CreateAsync(command);
            await messenger.SendAsync("spec-events", command);
            // send SB messageB

            return new AcceptedResult();
        }

        private static bool IsModified<TAny>(TAny current, TAny item)
        {
            return JsonConvert.SerializeObject(current) == JsonConvert.SerializeObject(item);
        }

    }
}