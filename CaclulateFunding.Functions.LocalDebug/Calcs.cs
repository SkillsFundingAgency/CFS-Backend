using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;

namespace CaclulateFunding.Functions.LocalDebug
{
    public static class Function1
    {
        [FunctionName("calc-events-create-draft")]
        public static void Run([QueueTrigger("on-calc-events-create-draft", Connection = "UseDevelopmentStorage=true")]string myQueueItem, TraceWriter log)
        {
            log.Info($"C# Queue trigger function processed: {myQueueItem}");
        }
    }
}
