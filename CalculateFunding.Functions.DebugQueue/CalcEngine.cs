using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Functions.CalcEngine.ServiceBus;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Extensions;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CalculateFunding.Functions.DebugQueue
{
    public static class CalcEngine
    {
        [FunctionName("on-calcs-generate-allocations-event")]
        public static async Task RunOnCalcsCreateDraftEvent(
            [QueueTrigger(ServiceBusConstants.QueueNames.CalcEngineGenerateAllocationResults, Connection = "AzureConnectionString")] string item, ILogger log)
        {
            using (IServiceScope scope = Functions.CalcEngine.Startup.RegisterComponents(new ServiceCollection()).CreateScope())
            {
                Message message = Helpers.ConvertToMessage<string>(item);

                OnCalcsGenerateAllocationResults function = scope.ServiceProvider.GetService<OnCalcsGenerateAllocationResults>();

                await function.Run(message);

                log.LogInformation($"C# Queue trigger function processed: {item}");
            }
        }

        [FunctionName("on-calcs-generate-allocations-event-poisoned")]
        public static async Task RunOnCalculationGenerateFailure([QueueTrigger(ServiceBusConstants.QueueNames.CalcEngineGenerateAllocationResultsPoisonedLocal, Connection = "AzureConnectionString")] string item, ILogger log)
        {
            using (IServiceScope scope = Functions.CalcEngine.Startup.RegisterComponents(new ServiceCollection()).CreateScope())
            {
                Message message = Helpers.ConvertToMessage<string>(item);

                OnCalculationGenerateFailure function = scope.ServiceProvider.GetService<OnCalculationGenerateFailure>();

                await function.Run(message);

                log.LogInformation($"C# Queue trigger function processed: {item}");
            }
        }
    }
}
