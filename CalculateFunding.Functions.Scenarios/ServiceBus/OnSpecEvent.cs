using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;

namespace CalculateFunding.Functions.Scenarios.ServiceBus
{
    public static class OnSpecEvent
    {
        [FunctionName("on-spec-event")]
        public static void Run(
            [ServiceBusTrigger("specs-events", "specs-events-scenarios", Connection = "ServiceBusConnection")]
            string messageJson,
            TraceWriter log)
        {
            log.Info($"C# ServiceBus queue trigger function processed message: {messageJson}");
        }

    }
}
