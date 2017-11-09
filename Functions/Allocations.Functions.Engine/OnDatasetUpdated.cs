using Microsoft.Azure.Documents;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Allocations.Functions.Engine
{
    public static class OnDatasetUpdated
    {
        [FunctionName("on-dataset-updated")]
        public static async Task Run(
            [CosmosDBTrigger(
            "allocations", 
            "datasets", 
            ConnectionStringSetting = "CosmosDBConnectionString",
            LeaseCollectionName = "datasets-lease", 
            CreateLeaseCollectionIfNotExists = true)]
            IReadOnlyList<Document> input,
            TraceWriter log)
        {


            log.Verbose("Document count " + input?.Count);
            log.Verbose("First document Id " + input?[0]?.Id);
        }


    }
}
