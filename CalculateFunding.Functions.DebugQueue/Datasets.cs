using System.Globalization;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Functions.Datasets;
using CalculateFunding.Functions.Datasets.ServiceBus;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Models.Datasets.Converter;
using CalculateFunding.Services.Core.Constants;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using GetDatasetBlobModel = CalculateFunding.Models.Datasets.GetDatasetBlobModel;
using SpecificationConverterMergeRequest = CalculateFunding.Common.ApiClient.DataSets.Models.SpecificationConverterMergeRequest;

namespace CalculateFunding.Functions.DebugQueue
{
    public static class Datasets
    {
        [FunctionName("on-dataset-event")]
        public static async Task RunPublishProviderResults([QueueTrigger(ServiceBusConstants.QueueNames.ProcessDataset, Connection = "AzureConnectionString")]
            string item,
            ILogger log)
        {
            using IServiceScope scope = Startup.RegisterComponents(new ServiceCollection()).CreateScope();

            CultureInfo.CurrentCulture = new CultureInfo("en-GB");
            Message message = Helpers.ConvertToMessage<Dataset>(item);

            OnDatasetEvent function = scope.ServiceProvider.GetService<OnDatasetEvent>();

            Guard.ArgumentNotNull(function, nameof(OnDatasetEvent));

            await function.Run(message);

            log.LogInformation($"C# Queue trigger function processed: {item}");
        }

        [FunctionName("on-delete-datasets")]
        public static async Task RunDeleteDatasets([QueueTrigger(ServiceBusConstants.QueueNames.DeleteDatasets, Connection = "AzureConnectionString")]
            string item,
            ILogger log)
        {
            using IServiceScope scope = Startup.RegisterComponents(new ServiceCollection()).CreateScope();

            CultureInfo.CurrentCulture = new CultureInfo("en-GB");
            Message message = Helpers.ConvertToMessage<string>(item);

            OnDeleteDatasets function = scope.ServiceProvider.GetService<OnDeleteDatasets>();

            await function.Run(message);

            log.LogInformation($"C# Queue trigger function processed: {item}");
        }

        [FunctionName("on-dataset-event-poisoned")]
        public static async Task RunPublishProviderResultsFailure([QueueTrigger(ServiceBusConstants.QueueNames.ProcessDatasetPoisonedLocal, Connection = "AzureConnectionString")]
            string item,
            ILogger log)
        {
            using IServiceScope scope = Startup.RegisterComponents(new ServiceCollection()).CreateScope();

            Message message = Helpers.ConvertToMessage<Dataset>(item);

            OnDatasetEventFailure function = scope.ServiceProvider.GetService<OnDatasetEventFailure>();

            Guard.ArgumentNotNull(function, nameof(OnDatasetEventFailure));

            await function.Run(message);

            log.LogInformation($"C# Queue trigger function processed: {item}");
        }

        [FunctionName("on-dataset-validation-event")]
        public static async Task RunValidateDatasetEvent([QueueTrigger(ServiceBusConstants.QueueNames.ValidateDataset, Connection = "AzureConnectionString")]
            string item,
            ILogger log)
        {
            using IServiceScope scope = Startup.RegisterComponents(new ServiceCollection()).CreateScope();

            CultureInfo.CurrentCulture = new CultureInfo("en-GB");
            Message message = Helpers.ConvertToMessage<GetDatasetBlobModel>(item);

            OnDatasetValidationEvent function = scope.ServiceProvider.GetService<OnDatasetValidationEvent>();

            Guard.ArgumentNotNull(function, nameof(OnDatasetValidationEvent));

            await function.Run(message);

            log.LogInformation($"C# Queue trigger function processed: {item}");
        }

        [FunctionName("on-dataset-validation-event-poisoned")]
        public static async Task RunOnValidateDatasetsFailure([QueueTrigger(ServiceBusConstants.QueueNames.ValidateDatasetPoisonedLocal, Connection = "AzureConnectionString")]
            string item,
            ILogger log)
        {
            using IServiceScope scope = Startup.RegisterComponents(new ServiceCollection()).CreateScope();

            Message message = Helpers.ConvertToMessage<string>(item);

            OnDatasetValidationEventFailure function = scope.ServiceProvider.GetService<OnDatasetValidationEventFailure>();

            Guard.ArgumentNotNull(function, nameof(OnDatasetValidationEventFailure));

            await function.Run(message);

            log.LogInformation($"C# Queue trigger function processed: {item}");
        }

        [FunctionName(FunctionConstants.MapFdzDatasets)]
        public static async Task RunOnMapFdzDatasetsEventFired([QueueTrigger(ServiceBusConstants.QueueNames.MapFdzDatasets, Connection = "AzureConnectionString")]
            string item,
            ILogger log)
        {
            using IServiceScope scope = Startup.RegisterComponents(new ServiceCollection()).CreateScope();

            Message message = Helpers.ConvertToMessage<Dataset>(item);

            OnMapFdzDatasetsEventFired function = scope.ServiceProvider.GetService<OnMapFdzDatasetsEventFired>();

            Guard.ArgumentNotNull(function, nameof(OnMapFdzDatasetsEventFired));

            await function.Run(message);

            log.LogInformation($"C# Queue trigger function processed: {item}");
        }

        [FunctionName(FunctionConstants.MapFdzDatasetsPoisoned)]
        public static async Task RunOnMapFdzDatasetsEventFiredFailure([QueueTrigger(ServiceBusConstants.QueueNames.MapFdzDatasetsPoisonedLocal, Connection = "AzureConnectionString")]
            string item,
            ILogger log)
        {
            using IServiceScope scope = Startup.RegisterComponents(new ServiceCollection()).CreateScope();

            Message message = Helpers.ConvertToMessage<Dataset>(item);

            OnMapFdzDatasetsEventFiredFailure function = scope.ServiceProvider.GetService<OnMapFdzDatasetsEventFiredFailure>();

            Guard.ArgumentNotNull(function, nameof(OnMapFdzDatasetsEventFiredFailure));

            await function.Run(message);

            log.LogInformation($"C# Queue trigger function processed: {item}");
        }

        [FunctionName(OnRunConverterDataMerge.FunctionName)]
        public static async Task RunOnRunConverterDataMerge([QueueTrigger(ServiceBusConstants.QueueNames.RunConverterDatasetMerge, Connection = "AzureConnectionString")]
            string item,
            ILogger log)
        {
            using IServiceScope scope = Startup.RegisterComponents(new ServiceCollection()).CreateScope();

            Message message = Helpers.ConvertToMessage<ConverterMergeRequest>(item);

            OnRunConverterDataMerge function = scope.ServiceProvider.GetService<OnRunConverterDataMerge>();

            Guard.ArgumentNotNull(function, nameof(OnMapFdzDatasetsEventFired));

            await function.Run(message);

            log.LogInformation($"C# Queue trigger function processed: {item}");
        }

        [FunctionName(OnRunConverterDataMergeFailure.FunctionName)]
        public static async Task RunOnRunConverterDataMergeFailure(
            [QueueTrigger(ServiceBusConstants.QueueNames.RunConverterDatasetMergePoisonedLocal, Connection = "AzureConnectionString")]
            string item,
            ILogger log)
        {
            using IServiceScope scope = Startup.RegisterComponents(new ServiceCollection()).CreateScope();

            Message message = Helpers.ConvertToMessage<ConverterMergeRequest>(item);

            OnRunConverterDataMergeFailure function = scope.ServiceProvider.GetService<OnRunConverterDataMergeFailure>();

            Guard.ArgumentNotNull(function, nameof(OnMapFdzDatasetsEventFiredFailure));

            await function.Run(message);

            log.LogInformation($"C# Queue trigger function processed: {item}");
        }

        [FunctionName(OnCreateSpecificationConverterDatasetsMerge.FunctionName)]
        public static async Task RunOnCreateSpecificationConverterDatasetsMerge([QueueTrigger(ServiceBusConstants.QueueNames.SpecificationConverterDatasetsMerge, Connection = "AzureConnectionString")]
            string item,
            ILogger log)
        {
            using IServiceScope scope = Startup.RegisterComponents(new ServiceCollection()).CreateScope();

            Message message = Helpers.ConvertToMessage<SpecificationConverterMergeRequest>(item);

            OnCreateSpecificationConverterDatasetsMerge function = scope.ServiceProvider.GetService<OnCreateSpecificationConverterDatasetsMerge>();

            Guard.ArgumentNotNull(function, nameof(OnCreateSpecificationConverterDatasetsMerge));

            await function.Run(message);

            log.LogInformation($"C# Queue trigger function processed: {item}");
        }

        [FunctionName(OnCreateSpecificationConverterDatasetsMergeFailure.FunctionName)]
        public static async Task RunOnCreateSpecificationConverterDatasetsMergeFailure(
            [QueueTrigger(ServiceBusConstants.QueueNames.SpecificationConverterDatasetsMergePoisonedLocal, Connection = "AzureConnectionString")]
            string item,
            ILogger log)
        {
            using IServiceScope scope = Startup.RegisterComponents(new ServiceCollection()).CreateScope();

            Message message = Helpers.ConvertToMessage<SpecificationConverterMergeRequest>(item);

            OnCreateSpecificationConverterDatasetsMergeFailure function = scope.ServiceProvider.GetService<OnCreateSpecificationConverterDatasetsMergeFailure>();

            Guard.ArgumentNotNull(function, nameof(OnCreateSpecificationConverterDatasetsMergeFailure));

            await function.Run(message);

            log.LogInformation($"C# Queue trigger function processed: {item}");
        }

        [FunctionName(OnConverterWizardActivityCsvGeneration.FunctionName)]
        public static async Task RunOnConverterWizardActivityCsvGeneration([QueueTrigger(ServiceBusConstants.QueueNames.ConverterWizardActivityCsvGeneration, Connection = "AzureConnectionString")]
            string item,
            ILogger log)
        {
            using IServiceScope scope = Startup.RegisterComponents(new ServiceCollection()).CreateScope();

            Message message = Helpers.ConvertToMessage<ConverterMergeRequest>(item);

            OnConverterWizardActivityCsvGeneration function = scope.ServiceProvider.GetService<OnConverterWizardActivityCsvGeneration>();

            Guard.ArgumentNotNull(function, nameof(OnConverterWizardActivityCsvGeneration));

            await function.Run(message);

            log.LogInformation($"C# Queue trigger function processed: {item}");
        }

        [FunctionName(OnConverterWizardActivityCsvGenerationFailure.FunctionName)]
        public static async Task RunOnConverterWizardActivityCsvGenerationFailure(
            [QueueTrigger(ServiceBusConstants.QueueNames.ConverterWizardActivityCsvGenerationPoisonedLocal, Connection = "AzureConnectionString")]
            string item,
            ILogger log)
        {
            using IServiceScope scope = Startup.RegisterComponents(new ServiceCollection()).CreateScope();

            Message message = Helpers.ConvertToMessage<ConverterMergeRequest>(item);

            OnConverterWizardActivityCsvGenerationFailure function = scope.ServiceProvider.GetService<OnConverterWizardActivityCsvGenerationFailure>();

            Guard.ArgumentNotNull(function, nameof(OnConverterWizardActivityCsvGenerationFailure));

            await function.Run(message);

            log.LogInformation($"C# Queue trigger function processed: {item}");
        }



        [FunctionName(OnProcessDatasetObsoleteItems.FunctionName)]
        public static async Task RunOnProcessDatasetObsoleteItems(
            [QueueTrigger(ServiceBusConstants.QueueNames.ProcessDatasetObsoleteItems, Connection = "AzureConnectionString")]
            string item,
            ILogger log)
        {
            using IServiceScope scope = Startup.RegisterComponents(new ServiceCollection()).CreateScope();

            Message message = Helpers.ConvertToMessage<string>(item);

            OnProcessDatasetObsoleteItems function = scope.ServiceProvider.GetService<OnProcessDatasetObsoleteItems>();

            Guard.ArgumentNotNull(function, nameof(OnProcessDatasetObsoleteItems));

            await function.Run(message);

            log.LogInformation($"C# Queue trigger function processed: {item}");
        }

        [FunctionName(OnProcessDatasetObsoleteItemsFailure.FunctionName)]
        public static async Task RunOnProcessDatasetObsoleteItemsFailure(
            [QueueTrigger(ServiceBusConstants.QueueNames.ProcessDatasetObsoleteItemsPoisonedLocal, Connection = "AzureConnectionString")]
            string item,
            ILogger log)
        {
            using IServiceScope scope = Startup.RegisterComponents(new ServiceCollection()).CreateScope();

            Message message = Helpers.ConvertToMessage<string>(item);

            OnProcessDatasetObsoleteItemsFailure function = scope.ServiceProvider.GetService<OnProcessDatasetObsoleteItemsFailure>();

            Guard.ArgumentNotNull(function, nameof(OnProcessDatasetObsoleteItemsFailure));

            await function.Run(message);

            log.LogInformation($"C# Queue trigger function processed: {item}");
        }
    }
}