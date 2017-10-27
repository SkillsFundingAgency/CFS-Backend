using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;

namespace Allocations.Functions.Specs
{
    public static class HealthCheck
    {
        [FunctionName("HealthCheck")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)]HttpRequestMessage req, TraceWriter log)
        {
            var versionNumber = Assembly.GetExecutingAssembly().GetName().Version;
            return req.CreateResponse(HttpStatusCode.OK, JsonConvert.SerializeObject(new { versionNumber }));
        }
    }
}
