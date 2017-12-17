using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;

namespace CalculateFunding.Functions.Scenarios.ServiceBus
{
    public static class OnCalcEvent
    {
        [FunctionName("on-calc-event")]
        public static void Run(
            [ServiceBusTrigger("calcs-events", "calcs-events-scenarios", Connection = "ServiceBusConnection")]
            string messageJson,
            TraceWriter log)
        {
            log.Info($"C# ServiceBus queue trigger function processed message: {messageJson}");
        }

    }
}
