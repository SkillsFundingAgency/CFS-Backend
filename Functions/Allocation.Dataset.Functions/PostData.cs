using System.Linq;
using System.Net;
using System.Net.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;

namespace Allocation.Dataset.Functions
{
    public static class PostData
    {
        [FunctionName("PostData")]
        public static HttpResponseMessage Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = "datasets/{modelName}/{datasetName}/{ukprn}/")]HttpRequestMessage req, string modelName, string datasetName, string ukprn, TraceWriter log)
        {
            log.Info("C# HTTP trigger function processed a request.");

            // Fetching the name from the path parameter in the request URL
            return req.CreateResponse(HttpStatusCode.OK, $"Hello {modelName}-{datasetName}-{ukprn}");
        }
    }
}
