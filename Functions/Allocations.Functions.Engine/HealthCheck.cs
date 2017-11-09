using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Allocations.Models.Specs;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Allocations.Functions.Engine
{
    public static class HealthCheck
    {
        [FunctionName("HealthCheck")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)]HttpRequestMessage req, TraceWriter log)
        {
            var versionNumber = typeof(Budget).Assembly.GetName().Version;

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonConvert.SerializeObject(new
                    {
                        versionNumber
                    },
                    new JsonSerializerSettings
                    {
                        ContractResolver = new CamelCasePropertyNamesContractResolver(),
                        Formatting = Formatting.Indented
                    }), System.Text.Encoding.UTF8, "application/json")
            };
        }
    }
}
