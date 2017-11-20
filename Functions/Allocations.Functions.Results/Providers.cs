using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using Allocations.Models.Results;
using Allocations.Repository;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Allocations.Functions.Results
{
    public static class Providers
    {
        [FunctionName("providers")]
        public static HttpResponseMessage Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequestMessage req, TraceWriter log)
        {
            string budgetId = req.GetQueryNameValuePairs()
                .FirstOrDefault(q => String.Compare(q.Key, "budgetId", StringComparison.OrdinalIgnoreCase) == 0)
                .Value;

            if (budgetId == null)
            {
                return req.CreateResponse(HttpStatusCode.BadRequest, "budgetId is required");
            }

            string providerId = req.GetQueryNameValuePairs()
                .FirstOrDefault(q => String.Compare(q.Key, "providerId", StringComparison.OrdinalIgnoreCase) == 0)
                .Value;

            using (var repository = new Repository<ProviderTestResult>("results"))
            {
                if (!string.IsNullOrWhiteSpace(providerId))
                {
                    var result = repository.Query().Where(x => x.Budget.Id == budgetId && x.Provider.Id == providerId)
                        .ToList().FirstOrDefault();
                    if (result == null)
                    {
                        return req.CreateResponse(HttpStatusCode.NotFound);
                    }
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(JsonConvert.SerializeObject(result,
                            new JsonSerializerSettings
                            {
                                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                                Formatting = Formatting.Indented
                            }), System.Text.Encoding.UTF8, "application/json")
                    };
                }
                var results = repository.Query().Where(x => x.Budget.Id == budgetId).ToList();
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(JsonConvert.SerializeObject(results,
                        new JsonSerializerSettings
                        {
                            ContractResolver = new CamelCasePropertyNamesContractResolver(),
                            Formatting = Formatting.Indented
                        }), System.Text.Encoding.UTF8, "application/json")
                };

            }



        }

    }
}
