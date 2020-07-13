using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.ServiceBus;
using CalculateFunding.Functions.Results.ServiceBus;
using CalculateFunding.Functions.Results.Timer;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Results.Models;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Timers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CalculateFunding.Functions.DebugQueue
{
    public static class Results
    {
        [FunctionName("on-merge-specification-information-for-provider-with-results")]
        public static async Task RunOnMergeSpecificationInformationForProviderWithResults([QueueTrigger(ServiceBusConstants.QueueNames.MergeSpecificationInformationForProvider, 
            Connection = "AzureConnectionString")] string item, ILogger log)
        {
            using IServiceScope scope = Functions.Results.Startup.RegisterComponents(new ServiceCollection()).CreateScope();
            
            //the message helper was throwing newtonsoft exceptions so gone with this
            QueueMessage<SpecificationInformation> queueMessage = item.AsPoco<QueueMessage<SpecificationInformation>>();
            
            Message message = new Message(queueMessage.Data.AsJsonBytes());
            
            foreach(KeyValuePair<string, string> property in queueMessage.UserProperties)
            {
                message.UserProperties.Add(property.Key, property.Value);
            }
            
            OnMergeSpecificationInformationForProviderWithResults function = scope.ServiceProvider.GetService<OnMergeSpecificationInformationForProviderWithResults>();

            await function.Run(message);

            log.LogInformation($"C# Queue trigger function processed: {item}");
        }
        
        [FunctionName("on-merge-specification-information-for-provider-with-results-failure")]
        public static async Task RunOnMergeSpecificationInformationForProviderWithResultsFailure([QueueTrigger(ServiceBusConstants.QueueNames.MergeSpecificationInformationForProviderPoisonedLocal, 
            Connection = "AzureConnectionString")] string item, ILogger log)
        {
            using IServiceScope scope = Functions.Results.Startup.RegisterComponents(new ServiceCollection()).CreateScope();
            
            //the message helper was throwing newtonsoft exceptions so gone with this
            QueueMessage<SpecificationInformation> queueMessage = item.AsPoco<QueueMessage<SpecificationInformation>>();
            
            Message message = new Message(queueMessage.Data.AsJsonBytes());
            
            foreach(KeyValuePair<string, string> property in queueMessage.UserProperties)
            {
                message.UserProperties.Add(property.Key, property.Value);
            }

            OnMergeSpecificationInformationForProviderWithResultsFailure function = scope.ServiceProvider.GetService<OnMergeSpecificationInformationForProviderWithResultsFailure>();

            await function.Run(message);

            log.LogInformation($"C# Queue trigger function processed: {item}");
        }
        
        [FunctionName("on-reindex-calculation-results")]
        public static async Task RunReIndexCalculationResults([QueueTrigger(ServiceBusConstants.QueueNames.ReIndexCalculationResultsIndex, Connection = "AzureConnectionString")] string item, ILogger log)
        {
            using IServiceScope scope = Functions.Results.Startup.RegisterComponents(new ServiceCollection()).CreateScope();
            
            Message message = Helpers.ConvertToMessage<string>(item);

            OnReIndexCalculationResults function = scope.ServiceProvider.GetService<OnReIndexCalculationResults>();

            await function.Run(message);

            log.LogInformation($"C# Queue trigger function processed: {item}");
        }

        [FunctionName("on-calculation-results-csv-generation")]
        public static async Task RunCalculationResultsCsvGeneration([QueueTrigger(ServiceBusConstants.QueueNames.CalculationResultsCsvGeneration, Connection = "AzureConnectionString")] string item, ILogger log)
        {
            using IServiceScope scope = Functions.Results.Startup.RegisterComponents(new ServiceCollection()).CreateScope();
            
            Message message = Helpers.ConvertToMessage<string>(item);

            OnCalculationResultsCsvGeneration function = scope.ServiceProvider.GetService<OnCalculationResultsCsvGeneration>();

            await function.Run(message);

            log.LogInformation($"C# Queue trigger function processed: {item}");
        }

        [FunctionName("on-calculation-results-csv-generation-timer")]
        public static async Task RunCalculationResultsCsvGenerationTimer([QueueTrigger(ServiceBusConstants.QueueNames.CalculationResultsCsvGenerationTimer, Connection = "AzureConnectionString")] string item, ILogger log)
        {
            using IServiceScope scope = Functions.Results.Startup.RegisterComponents(new ServiceCollection()).CreateScope();
            
            TimerInfo timerInfo = new TimerInfo(null, new ScheduleStatus());

            OnCalculationResultsCsvGenerationTimer function = scope.ServiceProvider.GetService<OnCalculationResultsCsvGenerationTimer>();

            await function.Run(timerInfo);

            log.LogInformation($"C# Queue trigger function processed: {item}");
        }
    }
}
