using System.Threading.Tasks;
using CalculateFunding.Functions.CosmosDbScaling.Timer;
using CalculateFunding.Services.Core.Constants;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Timers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CalculateFunding.Functions.DebugQueue
{
    public static class CosmosDbScaling
    {
        [FunctionName("on-scale-down-cosmosdb-collection")]
        public static async Task RunOnScaleDownCosmosdbCollection([QueueTrigger(ServiceBusConstants.QueueNames.ScaleDownCosmosdbCollection, Connection = "AzureConnectionString")] string item, ILogger log)
        {
            using (IServiceScope scope = Functions.CosmosDbScaling.Startup.RegisterComponents(new ServiceCollection()).CreateScope())
            {
                TimerInfo timerInfo = new TimerInfo(null, new ScheduleStatus());

                OnScaleDownCosmosDbCollection function = scope.ServiceProvider.GetService<OnScaleDownCosmosDbCollection>();

                await function.Run(timerInfo);

                log.LogInformation($"C# Queue trigger function processed: {item}");
            }
        }

        [FunctionName("on-incremental-scale-down-cosmosdb-collection")]
        public static async Task RunOnIncrementalScaleDownCosmosdbCollection([QueueTrigger(ServiceBusConstants.QueueNames.IncrementalScaleDownCosmosdbCollection, Connection = "AzureConnectionString")] string item, ILogger log)
        {
            using (IServiceScope scope = Functions.CosmosDbScaling.Startup.RegisterComponents(new ServiceCollection()).CreateScope())
            {
                TimerInfo timerInfo = new TimerInfo(null, new ScheduleStatus());

                OnIncrementalScaleDownCosmosDbCollection function = scope.ServiceProvider.GetService<OnIncrementalScaleDownCosmosDbCollection>();

                await function.Run(timerInfo);

                log.LogInformation($"C# Queue trigger function processed: {item}");
            }
        }
    }
}
