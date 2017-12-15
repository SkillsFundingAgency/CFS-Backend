using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;

namespace CalculateFunding.Functions.Specs.ServiceBus
{
    public static class OnMessage
    {
        [FunctionName("ServiceBusQueueTriggerCSharp")]
        public static void Run(
            [ServiceBusTrigger("SpecificationEvents", "SpecificationEvents-Specs", Connection = "ServiceBusConnection")]
            string myQueueItem,
            TraceWriter log)
        {
            log.Info($"C# ServiceBus queue trigger function processed message: {myQueueItem}");
        }

    }
}
