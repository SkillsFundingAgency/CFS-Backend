using System.Threading.Tasks;
using CalculateFunding.Functions.Scenarios.ServiceBus;
using CalculateFunding.Functions.TestEngine.ServiceBus;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Services.Core.Constants;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CalculateFunding.Functions.DebugQueue
{
    public static class Scenarios
    {
        [FunctionName("on-delete-tests")]
        public static async Task RunDeleteTests([QueueTrigger(ServiceBusConstants.QueueNames.DeleteTests, Connection = "AzureConnectionString")] string item, ILogger log)
        {
            using (IServiceScope scope = Functions.Scenarios.Startup.RegisterComponents(new ServiceCollection()).CreateScope())
            {
                Message message = Helpers.ConvertToMessage<string>(item);

                OnDeleteTests function = scope.ServiceProvider.GetService<OnDeleteTests>();

                await function.Run(message);

                log.LogInformation($"C# Queue trigger function processed: {item}");
            }
        }
    }
}
