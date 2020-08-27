using System.Globalization;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Functions.Datasets.ServiceBus;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Services.Core.Constants;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CalculateFunding.Functions.DebugQueue
{
    public static class Datasets
    {
        [FunctionName("on-dataset-event")]
        public static async Task RunPublishProviderResults([QueueTrigger(ServiceBusConstants.QueueNames.ProcessDataset, Connection = "AzureConnectionString")] string item, ILogger log)
        {
            using (IServiceScope scope = Functions.Datasets.Startup.RegisterComponents(new ServiceCollection()).CreateScope())
            {
                CultureInfo.CurrentCulture = new CultureInfo("en-GB");
                Message message = Helpers.ConvertToMessage<Dataset>(item);

                OnDatasetEvent function = scope.ServiceProvider.GetService<OnDatasetEvent>();

                Guard.ArgumentNotNull(function, nameof(OnDatasetEvent));
                
                await function.Run(message);

                log.LogInformation($"C# Queue trigger function processed: {item}");
            }
        }

        [FunctionName("on-dataset-event-poisoned")]
        public static async Task RunPublishProviderResultsFailure([QueueTrigger(ServiceBusConstants.QueueNames.ProcessDatasetPoisonedLocal, Connection = "AzureConnectionString")] string item, ILogger log)
        {
            using (IServiceScope scope = Functions.Datasets.Startup.RegisterComponents(new ServiceCollection()).CreateScope())
            {
                Message message = Helpers.ConvertToMessage<string>(item);

                OnDatasetEventFailure function = scope.ServiceProvider.GetService<OnDatasetEventFailure>();
                
                Guard.ArgumentNotNull(function, nameof(OnDatasetEventFailure));

                await function.Run(message);

                log.LogInformation($"C# Queue trigger function processed: {item}");
            }
        }

        [FunctionName("on-dataset-validation-event")]
        public static async Task RunValidateDatasetEvent([QueueTrigger(ServiceBusConstants.QueueNames.ValidateDataset, Connection = "AzureConnectionString")] string item, ILogger log)
        {
            using (IServiceScope scope = Functions.Datasets.Startup.RegisterComponents(new ServiceCollection()).CreateScope())
            {
                CultureInfo.CurrentCulture = new CultureInfo("en-GB");
                Message message = Helpers.ConvertToMessage<GetDatasetBlobModel>(item);

                OnDatasetValidationEvent function = scope.ServiceProvider.GetService<OnDatasetValidationEvent>();
                
                Guard.ArgumentNotNull(function, nameof(OnDatasetValidationEvent));

                await function.Run(message);

                log.LogInformation($"C# Queue trigger function processed: {item}");
            }
        }

        [FunctionName("on-dataset-validation-event-poisoned")]
        public static async Task RunOnValidateDatasetsFailure([QueueTrigger(ServiceBusConstants.QueueNames.ValidateDatasetPoisonedLocal, Connection = "AzureConnectionString")] string item, ILogger log)
        {
            using (IServiceScope scope = Functions.Datasets.Startup.RegisterComponents(new ServiceCollection()).CreateScope())
            {
                Message message = Helpers.ConvertToMessage<string>(item);

                OnDatasetValidationEventFailure function = scope.ServiceProvider.GetService<OnDatasetValidationEventFailure>();

                Guard.ArgumentNotNull(function, nameof(OnDatasetValidationEventFailure));

                await function.Run(message);

                log.LogInformation($"C# Queue trigger function processed: {item}");
            }
        }

        [FunctionName(FunctionConstants.MapFdzDatasets)]
        public static async Task RunOnMapFdzDatasetsEventFired([QueueTrigger(ServiceBusConstants.QueueNames.MapFdzDatasets, Connection = "AzureConnectionString")] string item, ILogger log)
        {
            using (IServiceScope scope = Functions.Datasets.Startup.RegisterComponents(new ServiceCollection()).CreateScope())
            {
                Message message = Helpers.ConvertToMessage<string>(item);

                OnMapFdzDatasetsEventFired function = scope.ServiceProvider.GetService<OnMapFdzDatasetsEventFired>();

                Guard.ArgumentNotNull(function, nameof(OnMapFdzDatasetsEventFired));

                await function.Run(message);

                log.LogInformation($"C# Queue trigger function processed: {item}");
            }
        }
    }
}
