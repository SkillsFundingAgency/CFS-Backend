using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using CalculateFunding.Models.Specs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace CalculateFunding.Functions.Specs
{
    public static class HealthCheck
    {
        [FunctionName("health-check")]
        public static IActionResult Run([HttpTrigger(AuthorizationLevel.Function, "get", "post")]HttpRequest req, TraceWriter log)
        {
            var versionNumber = typeof(Budget).Assembly.GetName().Version;

            return new JsonResult(new
            {
                versionNumber
            });
        }
    }
}
