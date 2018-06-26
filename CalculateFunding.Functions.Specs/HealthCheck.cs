using CalculateFunding.Models.Specs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;

namespace CalculateFunding.Functions.Specs
{
    public static class HealthCheck
    {
        [FunctionName("health-check")]
        public static IActionResult Run([HttpTrigger(AuthorizationLevel.Function, "get", "post")]HttpRequest req, TraceWriter log)
        {
            var versionNumber = typeof(Specification).Assembly.GetName().Version;

            return new JsonResult(new
            {
                versionNumber
            });
        }
    }
}
