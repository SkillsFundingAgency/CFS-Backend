using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;

namespace CalculateFunding.Functions.Datasets.ServiceBus
{
    public static class OnSpecEvent
    {
        [FunctionName("on-spec-event")]
        public static void Run(
            [ServiceBusTrigger("spec-events", "spec-events-datasets", Connection = "ServiceBusConnection")]
            string messageJson,
            TraceWriter log)
        {
            log.Info($"C# ServiceBus queue trigger function processed message: {messageJson}");
        }

    }
}
