using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;

namespace CalculateFunding.Functions.Scenarios.ServiceBus
{
    public static class OnCalcEvent
    {
        [FunctionName("on-calc-event")]
        public static void Run(
            [ServiceBusTrigger("calc-events", "calc-events-scenarios", Connection = "ServiceBusConnectionString")]
            string messageJson,
            TraceWriter log)
        {
            log.Info($"C# ServiceBus queue trigger function processed message: {messageJson}");
        }

    }
}
