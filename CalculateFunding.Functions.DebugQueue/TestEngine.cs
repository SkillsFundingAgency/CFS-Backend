using System.Threading.Tasks;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Services.Core.Constants;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace CalculateFunding.Functions.DebugQueue
{
    public static class TestEngine
    {
        [FunctionName("on-test-execution-event")]
        public static async Task RunTests([QueueTrigger(ServiceBusConstants.QueueNames.TestEngineExecuteTests, Connection = "AzureConnectionString")] string item, ILogger log)
        {
            Message message = Helpers.ConvertToMessage<BuildProject>(item);

            await Functions.TestEngine.ServiceBus.OnTestExecution.Run(message);

            log.LogInformation($"C# Queue trigger function processed: {item}");
        }
    }
}
