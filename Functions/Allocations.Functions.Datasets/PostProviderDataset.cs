using System.Linq;
using System.Net;
using System.Net.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Allocations.Models.Framework;
using Microsoft.Azure.Documents.Client;
using System;
using Microsoft.Azure.Documents;

namespace Allocations.Functions.Datasets
{
    public static class PostProviderDataset
    {
        [FunctionName("PostProviderDataset")]
        public static async Task<HttpResponseMessage> RunAsync([HttpTrigger(AuthorizationLevel.Function, "post", Route = "datasets/{modelName}/providers/{urn}/{datasetName}")]HttpRequestMessage request, string modelName, string providerUrn, string datasetName, TraceWriter log)
        {


        }
    }
}
