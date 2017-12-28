using System.Threading.Tasks;
using CalculateFunding.Functions.Common;
using CalculateFunding.Models.Specs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;

namespace CalculateFunding.Functions.Specs.Http
{
    public static class Specifications
    {
        
        [FunctionName("specifications")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequest req, TraceWriter log)
        {
            var restMethods = new RestGetMethods<Specification>();
            return await restMethods.Run(req, log, "specificationId");
        }

        [FunctionName("specifications-commands")]
        public static async Task<IActionResult> RunCommands(
            [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req, TraceWriter log)
        {
            var restMethods = new RestCommandMethods<Specification, SpecificationCommand>();
            return await restMethods.Run(req, log);
        }
    }
}
