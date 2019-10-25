using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Functions.Results.ServiceBus;
using CalculateFunding.Functions.Results.Timer;
using CalculateFunding.Services.Core.Constants;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Timers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CalculateFunding.Functions.DebugQueue
{
    public static class Results
    {
        [FunctionName("on-reindex-calculation-results")]
        public static async Task RunReIndexCalculationResults([QueueTrigger(ServiceBusConstants.QueueNames.ReIndexCalculationResultsIndex, Connection = "AzureConnectionString")] string item, ILogger log)
        {
            using (IServiceScope scope = Functions.Results.Startup.RegisterComponents(new ServiceCollection()).CreateScope())
            {
                Message message = Helpers.ConvertToMessage<string>(item);

                OnReIndexCalculationResults function = scope.ServiceProvider.GetService<OnReIndexCalculationResults>();

                await function.Run(message);

                log.LogInformation($"C# Queue trigger function processed: {item}");
            }
        }

        [FunctionName("on-calculation-results-csv-generation")]
        public static async Task RunCalculationResultsCsvGeneration([QueueTrigger(ServiceBusConstants.QueueNames.CalculationResultsCsvGeneration, Connection = "AzureConnectionString")] string item, ILogger log)
        {
            using (IServiceScope scope = Functions.Results.Startup.RegisterComponents(new ServiceCollection()).CreateScope())
            {
                Message message = Helpers.ConvertToMessage<string>(item);

                OnCalculationResultsCsvGeneration function = scope.ServiceProvider.GetService<OnCalculationResultsCsvGeneration>();

                await function.Run(message);

                log.LogInformation($"C# Queue trigger function processed: {item}");
            }
        }

        [FunctionName("on-calculation-results-csv-generation-timer")]
        public static async Task RunCalculationResultsCsvGenerationTimer([QueueTrigger(ServiceBusConstants.QueueNames.CalculationResultsCsvGenerationTimer, Connection = "AzureConnectionString")] string item, ILogger log)
        {
            using (IServiceScope scope = Functions.Results.Startup.RegisterComponents(new ServiceCollection()).CreateScope())
            {
                TimerInfo timerInfo = new TimerInfo(null, new ScheduleStatus());

                OnCalculationResultsCsvGenerationTimer function = scope.ServiceProvider.GetService<OnCalculationResultsCsvGenerationTimer>();

                await function.Run(timerInfo);

                log.LogInformation($"C# Queue trigger function processed: {item}");
            }
        }
    }
}
