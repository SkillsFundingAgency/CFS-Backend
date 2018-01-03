using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;

namespace CalculateFunding.Functions.Calcs.ServiceBus
{
    public static class OnDatasetEvent
    {
        [FunctionName("on-spec-event")]
        public static void Run(
            [ServiceBusTrigger("dataset-events", "dataset-events-calcs", Connection = "ServiceBusConnectionString")]
            string messageJson,
            TraceWriter log)
        {
            log.Info($"C# ServiceBus queue trigger function processed message: {messageJson}");
        }

    }
}
