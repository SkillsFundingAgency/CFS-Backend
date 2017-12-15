using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;

namespace CalculateFunding.Functions.Specs.Timers
{
    public static class OnTimer
    {
        [FunctionName("OnTimer")]
        [return: ServiceBus("SpecificationEvents", Connection = "ServiceBusConnection")]
        public static string Run([TimerTrigger("0 */5 * * * *")]TimerInfo myTimer, TraceWriter log)
        {
            log.Info($"C# Timer trigger function executed at: {DateTime.Now}");

            return "Hello I am a message";
        }
    }
}
