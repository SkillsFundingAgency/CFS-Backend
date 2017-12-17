using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;

namespace CalculateFunding.Functions.Results.ServiceBus
{
    public static class OnCalcEvent
    {
        [FunctionName("on-calc-event")]
        public static void Run(
            [ServiceBusTrigger("calcs-events", "calcs-events-results", Connection = "ServiceBusConnection")]
            string messageJson,
            TraceWriter log)
        {
            log.Info($"C# ServiceBus queue trigger function processed message: {messageJson}");
        }

    }
}
