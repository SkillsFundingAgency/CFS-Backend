using CalculateFunding.Models.Calcs;
using CalculateFunding.Services.Core.Constants;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using System.Threading.Tasks;

namespace CalculateFunding.Functions.DebugQueue
{
    public static class TestEngine
    {
        [FunctionName("on-test-execution-event")]
        public static async Task RunTests([QueueTrigger(ServiceBusConstants.QueueNames.TestEngineExecuteTests, Connection = "AzureConnectionString")] string item, TraceWriter log)
        {
            Message message = Helpers.ConvertToMessage<BuildProject>(item);

            await Functions.TestEngine.ServiceBus.OnTestExecution.Run(message);

            log.Info($"C# Queue trigger function processed: {item}");
        }
    }
}
