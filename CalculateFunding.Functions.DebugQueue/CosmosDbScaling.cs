using CalculateFunding.Services.Core.Constants;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Timers;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace CalculateFunding.Functions.DebugQueue
{
    public static class CosmosDbScaling
    {
        [FunctionName("on-scale-down-cosmosdb-collection")]
        public static async Task RunOnScaleDownCosmosdbCollection([QueueTrigger(ServiceBusConstants.QueueNames.ScaleDownCosmosdbCollection, Connection = "AzureConnectionString")] string item, ILogger log)
        {
            TimerInfo timerInfo = new TimerInfo(null, new ScheduleStatus());

            await Functions.CosmosDbScaling.Timer.OnScaleDownCosmosDbCollection.Run(timerInfo);

            log.LogInformation($"C# Queue trigger function processed: {item}");
        }

        [FunctionName("on-incremental-scale-down-cosmosdb-collection")]
        public static async Task RunOnIncrementalScaleDownCosmosdbCollection([QueueTrigger(ServiceBusConstants.QueueNames.IncrementaScaleDownCosmosdbCollection, Connection = "AzureConnectionString")] string item, ILogger log)
        {
            TimerInfo timerInfo = new TimerInfo(null, new ScheduleStatus());

            await Functions.CosmosDbScaling.Timer.OnIncrementa1ScaleDownCosmosDbCollection.Run(timerInfo);

            log.LogInformation($"C# Queue trigger function processed: {item}");
        }
    }
}
