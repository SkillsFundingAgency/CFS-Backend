using System.Threading.Tasks;
using CalculateFunding.Functions.Common;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Models.Results;
using CalculateFunding.Repositories.Common.Search;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace CalculateFunding.Functions.Results.Http
{
    public static class SpecificationScopes
    {

        [FunctionName("specification-scopes")]
        public static async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)]HttpRequest req, ILogger log)
        {
            var restMethods = new RestGetMethods<SpecificationScope>();
            return await restMethods.Run(req, log, "specificationId");
        }

        [FunctionName("specification-scopes-commands")]
        public static async Task<IActionResult> RunCommands(
            [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req, ILogger log)
        {
            var restMethods = new RestCommandMethods<SpecificationScope, SpecificationScopeCommand>("results-events");
            return await restMethods.Run(req, log);
        }

    }
}
