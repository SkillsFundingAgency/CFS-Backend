using System.Threading.Tasks;
using CalculateFunding.Functions.Common;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Models.Specs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace CalculateFunding.Functions.Datasets.Http
{
    public static class SourceDefinitions
    {
        
        [FunctionName("source-definitions")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequest req, ILogger log)
        {
            var restMethods = new RestGetMethods<SourceDefinition>();
            return await restMethods.Run(req, log, "id");
        }

        [FunctionName("source-definitions-commands")]
        public static async Task<IActionResult> RunCommands(
            [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req, ILogger log)
        {
            var restMethods = new RestCommandMethods<SourceDefinition, SourceDefinitionCommand>("dataset-events");
            return await restMethods.Run(req, log);
        }
    }
}
