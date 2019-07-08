using System.Threading.Tasks;
using CalculateFunding.Functions.TestEngine.ServiceBus;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Services.Core.Constants;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CalculateFunding.Functions.DebugQueue
{
    public static class TestEngine
    {
        [FunctionName("on-test-execution-event")]
        public static async Task RunTests([QueueTrigger(ServiceBusConstants.QueueNames.TestEngineExecuteTests, Connection = "AzureConnectionString")] string item, ILogger log)
        {
            using (IServiceScope scope = Functions.TestEngine.Startup.RegisterComponents(new ServiceCollection()).CreateScope())
            {
                Message message = Helpers.ConvertToMessage<BuildProject>(item);

                OnTestExecution function = scope.ServiceProvider.GetService<OnTestExecution>();

                await function.Run(message);

                log.LogInformation($"C# Queue trigger function processed: {item}");
            }
        }
    }
}
