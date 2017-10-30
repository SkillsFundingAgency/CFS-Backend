using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.Documents.Client;
using System.Collections.Generic;
using System.Configuration;
using Allocation.Models;
using Allocations.Engine;
using Allocations.Gherkin.Vocab;
using Allocations.Models.Budgets;
using Allocations.Models.Datasets;
using Allocations.Models.Framework;
using Allocations.Models.Results;
using Allocations.Repository;
using AY1718.CSharp.Allocations;
using Microsoft.Azure.Documents;
using Newtonsoft.Json;

namespace Allocations.Functions.Engine
{
    public static class OnDatasetUpdated
    {
        [FunctionName("OnDatasetUpdated")]
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
