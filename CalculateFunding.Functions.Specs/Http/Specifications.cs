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
            [HttpTrigger(AuthorizationLevel.Function, "post", "get")] HttpRequest req, TraceWriter log)
        {
            return await RestMethods<Specification>.Run(req, log, "specificationId");
        }
    }
}
