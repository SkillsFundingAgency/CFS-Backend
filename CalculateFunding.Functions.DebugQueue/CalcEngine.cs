using CalculateFunding.Services.Core.Constants;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using System.Threading.Tasks;

namespace CalculateFunding.Functions.DebugQueue
{
    public static class CalcEngine
    {
        [FunctionName("on-calcs-generate-allocations-event")]
        public static async Task RunOnCalcsCreateDraftEvent([QueueTrigger(ServiceBusConstants.QueueNames.CalcEngineGenerateAllocationResults, Connection = "AzureConnectionString")] string item, TraceWriter log)
        {
            Message message = Helpers.ConvertToMessage<string>(item);

            await Functions.CalcEngine.ServiceBus.OnCalcsGenerateAllocationResults.Run(message);

            log.Info($"C# Queue trigger function processed: {item}");
        }

        [FunctionName("on-calcs-generate-allocations-event-poisoned")]
        public static async Task RunOnCalculationGenerateFailure([QueueTrigger(ServiceBusConstants.QueueNames.CalcEngineGenerateAllocationResultsPoisonedLocal, Connection = "AzureConnectionString")] string item, TraceWriter log)
        {
            Message message = Helpers.ConvertToMessage<string>(item);

            await Functions.CalcEngine.ServiceBus.OnCalculationGenerateFailure.Run(message);

            log.Info($"C# Queue trigger function processed: {item}");
        }
    }
}
