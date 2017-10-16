using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.Documents;
using System.Collections.Generic;

namespace Allocations.Functions.Engine
{
    public static class OnUpdatedRequested
    {
        [FunctionName("OnUpdatedRequested")]
        public static async Task Run([CosmosDBTrigger("allocations", "AllocationRequests", ConnectionStringSetting = "ConnectionString", LeaseCollectionName = "AllocationRequestsLeases", CreateLeaseCollectionIfNotExists = true)]IReadOnlyList<Document> input, TraceWriter log)
        {
            log.Verbose("Document count " + input.Count);
            log.Verbose("First document Id " + input[0].Id);
        }
    }
}
