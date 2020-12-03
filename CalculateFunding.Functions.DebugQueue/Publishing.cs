using System.Threading.Tasks;
using CalculateFunding.Functions.Publishing.ServiceBus;
using CalculateFunding.Services.Core.Constants;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CalculateFunding.Functions.DebugQueue
{
    public static class Publishing
    {
        [FunctionName(FunctionConstants.BatchPublishedProviderValidation)]
        public static async Task RunBatchPublishedProviderValidation([QueueTrigger(ServiceBusConstants.QueueNames.PublishingBatchPublishedProviderValidation, 
            Connection = "AzureConnectionString")] string item, ILogger log)
        {
            using IServiceScope scope = Functions.Publishing.Startup.RegisterComponents(new ServiceCollection()).CreateScope();
            Message message = Helpers.ConvertToMessage<string>(item);

            OnBatchPublishedProviderValidation function = scope.ServiceProvider.GetService<OnBatchPublishedProviderValidation>();

            await function.Run(message);

            log.LogInformation($"C# Queue trigger function processed: {item}");
        }
        
        [FunctionName(FunctionConstants.BatchPublishedProviderValidationPoisoned)]
        public static async Task RunBatchPublishedProviderValidationFailure([QueueTrigger(ServiceBusConstants.QueueNames.PublishingBatchPublishedProviderValidationPoisonedLocal, 
            Connection = "AzureConnectionString")] string item, ILogger log)
        {
            using IServiceScope scope = Functions.Publishing.Startup.RegisterComponents(new ServiceCollection()).CreateScope();
            Message message = Helpers.ConvertToMessage<string>(item);

            OnBatchPublishedProviderValidationFailure function = scope.ServiceProvider.GetService<OnBatchPublishedProviderValidationFailure>();

            await function.Run(message);

            log.LogInformation($"C# Queue trigger function processed: {item}");
        }
        
        [FunctionName("on-publishing-run-sql-import")]
        public static async Task RunSqlImport([QueueTrigger(ServiceBusConstants.QueueNames.PublishingRunSqlImport, 
            Connection = "AzureConnectionString")] string item, ILogger log)
        {
            using IServiceScope scope = Functions.Publishing.Startup.RegisterComponents(new ServiceCollection()).CreateScope();
            Message message = Helpers.ConvertToMessage<string>(item);

            OnRunSqlImport function = scope.ServiceProvider.GetService<OnRunSqlImport>();

            await function.Run(message);

            log.LogInformation($"C# Queue trigger function processed: {item}");
        }
        
        [FunctionName("on-published-funding-undo")]
        public static async Task RunUndoPublishedFunding([QueueTrigger(ServiceBusConstants.QueueNames.PublishedFundingUndo, 
            Connection = "AzureConnectionString")] string item, ILogger log)
        {
            using IServiceScope scope = Functions.Publishing.Startup.RegisterComponents(new ServiceCollection()).CreateScope();
            Message message = Helpers.ConvertToMessage<string>(item);

            OnPublishedFundingUndo function = scope.ServiceProvider.GetService<OnPublishedFundingUndo>();

            await function.Run(message);

            log.LogInformation($"C# Queue trigger function processed: {item}");
        }

        [FunctionName("on-publishing-generate-published-funding-csv")]
        public static async Task RunGeneratePublishedFundingCsv([QueueTrigger(ServiceBusConstants.QueueNames.GeneratePublishedFundingCsv, 
            Connection = "AzureConnectionString")] string item, ILogger log)
        {
            using IServiceScope scope = Functions.Publishing.Startup.RegisterComponents(new ServiceCollection()).CreateScope();
            Message message = Helpers.ConvertToMessage<string>(item);

            OnGeneratePublishedFundingCsv function = scope.ServiceProvider.GetService<OnGeneratePublishedFundingCsv>();

            await function.Run(message);

            log.LogInformation($"C# Queue trigger function processed: {item}");
        }
        
        [FunctionName("on-publishing-generate-published-funding-csv-failure")]
        public static async Task RunGeneratePublishedFundingCsvFailure([QueueTrigger(ServiceBusConstants.QueueNames.GeneratePublishedFundingCsvPoisonedLocal, 
            Connection = "AzureConnectionString")] string item, ILogger log)
        {
            using IServiceScope scope = Functions.Publishing.Startup.RegisterComponents(new ServiceCollection()).CreateScope();
            Message message = Helpers.ConvertToMessage<string>(item);

            OnGeneratePublishedFundingCsvFailure function = scope.ServiceProvider.GetService<OnGeneratePublishedFundingCsvFailure>();

            await function.Run(message);

            log.LogInformation($"C# Queue trigger function processed: {item}");
        }
        
        [FunctionName("on-publishing-delete-published-providers")]
        public static async Task RunDeletePublishedProviders([QueueTrigger(ServiceBusConstants.QueueNames.DeletePublishedProviders, 
            Connection = "AzureConnectionString")] string item, ILogger log)
        {
            using IServiceScope scope = Functions.Publishing.Startup.RegisterComponents(new ServiceCollection()).CreateScope();
            Message message = Helpers.ConvertToMessage<string>(item);

            OnDeletePublishedProviders function = scope.ServiceProvider.GetService<OnDeletePublishedProviders>();

            await function.Run(message);

            log.LogInformation($"C# Queue trigger function processed: {item}");
        }
        
        [FunctionName("on-publishing-reindex-published-providers")]
        public static async Task RunReIndexPublishedProviders([QueueTrigger(ServiceBusConstants.QueueNames.PublishingReIndexPublishedProviders, 
            Connection = "AzureConnectionString")] string item, ILogger log)
        {
            using IServiceScope scope = Functions.Publishing.Startup.RegisterComponents(new ServiceCollection()).CreateScope();
            Message message = Helpers.ConvertToMessage<string>(item);

            OnReIndexPublishedProviders function = scope.ServiceProvider.GetService<OnReIndexPublishedProviders>();

            await function.Run(message);

            log.LogInformation($"C# Queue trigger function processed: {item}");
        }

        [FunctionName("on-publishing-reindex-published-providers-poisoned")]
        public static async Task RunReIndexPublishedProvidersFailure([QueueTrigger(ServiceBusConstants.QueueNames.PublishingReIndexPublishedProvidersPoisonedLocal, 
                Connection = "AzureConnectionString")]
            string item, ILogger log)
        {
            using IServiceScope scope = Functions.Publishing.Startup.RegisterComponents(new ServiceCollection()).CreateScope();
            Message message = Helpers.ConvertToMessage<string>(item);

            OnReIndexPublishedProvidersFailure function = scope.ServiceProvider.GetService<OnReIndexPublishedProvidersFailure>();

            await function.Run(message);

            log.LogInformation($"C# Queue trigger function processed: {item}");
        }
        
        [FunctionName(FunctionConstants.PublishingApproveAllProviderFunding)]
        public static async Task RunApproveAllProviderFunding([QueueTrigger(ServiceBusConstants.QueueNames.PublishingApproveAllProviderFunding, Connection = "AzureConnectionString")] string item, ILogger log)
        {
            using IServiceScope scope = Functions.Publishing.Startup.RegisterComponents(new ServiceCollection()).CreateScope();
            Message message = Helpers.ConvertToMessage<string>(item);

            OnApproveAllProviderFunding function = scope.ServiceProvider.GetService<OnApproveAllProviderFunding>();

            await function.Run(message);

            log.LogInformation($"C# Queue trigger function processed: {item}");
        }

        [FunctionName(FunctionConstants.PublishingApproveAllProviderFundingPoisoned)]
        public static async Task RunApproveAllProviderFundingFailure([QueueTrigger(ServiceBusConstants.QueueNames.PublishingApproveAllProviderFundingPoisonedLocal, Connection = "AzureConnectionString")] string item, ILogger log)
        {
            using IServiceScope scope = Functions.Publishing.Startup.RegisterComponents(new ServiceCollection()).CreateScope();
            Message message = Helpers.ConvertToMessage<string>(item);

            OnApproveAllProviderFundingFailure function = scope.ServiceProvider.GetService<OnApproveAllProviderFundingFailure>();

            await function.Run(message);

            log.LogInformation($"C# Queue trigger function processed: {item}");
        }

        [FunctionName(FunctionConstants.PublishingApproveBatchProviderFunding)]
        public static async Task RunApproveBatchProviderFunding([QueueTrigger(ServiceBusConstants.QueueNames.PublishingApproveBatchProviderFunding, Connection = "AzureConnectionString")] string item, ILogger log)
        {
            using IServiceScope scope = Functions.Publishing.Startup.RegisterComponents(new ServiceCollection()).CreateScope();
            Message message = Helpers.ConvertToMessage<string>(item);

            OnApproveBatchProviderFunding function = scope.ServiceProvider.GetService<OnApproveBatchProviderFunding>();

            await function.Run(message);

            log.LogInformation($"C# Queue trigger function processed: {item}");
        }

        [FunctionName(FunctionConstants.PublishingApproveBatchProviderFundingPoisoned)]
        public static async Task RunApproveBatchProviderFundingFailure([QueueTrigger(ServiceBusConstants.QueueNames.PublishingApproveBatchProviderFundingPoisonedLocal, Connection = "AzureConnectionString")] string item, ILogger log)
        {
            using IServiceScope scope = Functions.Publishing.Startup.RegisterComponents(new ServiceCollection()).CreateScope();
            Message message = Helpers.ConvertToMessage<string>(item);

            OnApproveBatchProviderFundingFailure function = scope.ServiceProvider.GetService<OnApproveBatchProviderFundingFailure>();

            await function.Run(message);

            log.LogInformation($"C# Queue trigger function processed: {item}");
        }

        [FunctionName("on-publishing-refresh-funding")]
        public static async Task RunRefreshFunding([QueueTrigger(ServiceBusConstants.QueueNames.PublishingRefreshFunding, Connection = "AzureConnectionString")] string item, ILogger log)
        {
            using IServiceScope scope = Functions.Publishing.Startup.RegisterComponents(new ServiceCollection()).CreateScope();
            Message message = Helpers.ConvertToMessage<string>(item);

            OnRefreshFunding function = scope.ServiceProvider.GetService<OnRefreshFunding>();

            await function.Run(message);

            log.LogInformation($"C# Queue trigger function processed: {item}");
        }

        [FunctionName("on-publishing-refresh-funding-poisoned")]
        public static async Task RunRefreshFundingFailure([QueueTrigger(ServiceBusConstants.QueueNames.PublishingRefreshFundingPoisonedLocal, Connection = "AzureConnectionString")] string item, ILogger log)
        {
            using IServiceScope scope = Functions.Publishing.Startup.RegisterComponents(new ServiceCollection()).CreateScope();
            Message message = Helpers.ConvertToMessage<string>(item);

            OnRefreshFundingFailure function = scope.ServiceProvider.GetService<OnRefreshFundingFailure>();

            await function.Run(message);

            log.LogInformation($"C# Queue trigger function processed: {item}");
        }

        [FunctionName(FunctionConstants.PublishingPublishAllProviderFunding)]
        public static async Task RunPublishAllProviderFunding([QueueTrigger(ServiceBusConstants.QueueNames.PublishingPublishAllProviderFunding, Connection = "AzureConnectionString")] string item, ILogger log)
        {
            using IServiceScope scope = Functions.Publishing.Startup.RegisterComponents(new ServiceCollection()).CreateScope();
            Message message = Helpers.ConvertToMessage<string>(item);

            OnPublishAllProviderFunding function = scope.ServiceProvider.GetService<OnPublishAllProviderFunding>();

            await function.Run(message);

            log.LogInformation($"C# Queue trigger function processed: {item}");
        }

        [FunctionName(FunctionConstants.PublishingPublishAllProviderFundingPoisoned)]
        public static async Task RunPublishAllProviderFundingFailure([QueueTrigger(ServiceBusConstants.QueueNames.PublishingPublishAllProviderFundingPoisonedLocal, Connection = "AzureConnectionString")] string item, ILogger log)
        {
            using IServiceScope scope = Functions.Publishing.Startup.RegisterComponents(new ServiceCollection()).CreateScope();
            Message message = Helpers.ConvertToMessage<string>(item);

            OnPublishAllProviderFundingFailure function = scope.ServiceProvider.GetService<OnPublishAllProviderFundingFailure>();

            await function.Run(message);

            log.LogInformation($"C# Queue trigger function processed: {item}");
        }

        [FunctionName(FunctionConstants.PublishIntegrityCheck)]
        public static async Task RunPublishIntegrityCheck([QueueTrigger(ServiceBusConstants.QueueNames.PublishIntegrityCheck, Connection = "AzureConnectionString")] string item, ILogger log)
        {
            using IServiceScope scope = Functions.Publishing.Startup.RegisterComponents(new ServiceCollection()).CreateScope();
            Message message = Helpers.ConvertToMessage<string>(item);

            OnPublishIntegrityCheck function = scope.ServiceProvider.GetService<OnPublishIntegrityCheck>();

            await function.Run(message);

            log.LogInformation($"C# Queue trigger function processed: {item}");
        }

        [FunctionName(FunctionConstants.PublishIntegrityCheckPoisoned)]
        public static async Task RunPublishIntegrityCheckFailure([QueueTrigger(ServiceBusConstants.QueueNames.PublishIntegrityCheckPoisonedLocal, Connection = "AzureConnectionString")] string item, ILogger log)
        {
            using IServiceScope scope = Functions.Publishing.Startup.RegisterComponents(new ServiceCollection()).CreateScope();
            Message message = Helpers.ConvertToMessage<string>(item);

            OnPublishIntegrityCheckFailure function = scope.ServiceProvider.GetService<OnPublishIntegrityCheckFailure>();

            await function.Run(message);

            log.LogInformation($"C# Queue trigger function processed: {item}");
        }

        [FunctionName(FunctionConstants.PublishingPublishBatchProviderFunding)]
        public static async Task RunPublishBatchProviderFunding([QueueTrigger(ServiceBusConstants.QueueNames.PublishingPublishBatchProviderFunding, Connection = "AzureConnectionString")] string item, ILogger log)
        {
            using IServiceScope scope = Functions.Publishing.Startup.RegisterComponents(new ServiceCollection()).CreateScope();
            Message message = Helpers.ConvertToMessage<string>(item);

            OnPublishBatchProviderFunding function = scope.ServiceProvider.GetService<OnPublishBatchProviderFunding>();

            await function.Run(message);

            log.LogInformation($"C# Queue trigger function processed: {item}");
        }

        [FunctionName(FunctionConstants.PublishingPublishBatchProviderFundingPoisoned)]
        public static async Task RunPublishBatchProviderFundingFailure([QueueTrigger(ServiceBusConstants.QueueNames.PublishingPublishBatchProviderFundingPoisonedLocal, Connection = "AzureConnectionString")] string item, ILogger log)
        {
            using IServiceScope scope = Functions.Publishing.Startup.RegisterComponents(new ServiceCollection()).CreateScope();
            Message message = Helpers.ConvertToMessage<string>(item);

            OnPublishBatchProviderFundingFailure function = scope.ServiceProvider.GetService<OnPublishBatchProviderFundingFailure>();

            await function.Run(message);

            log.LogInformation($"C# Queue trigger function processed: {item}");
        }

        [FunctionName("on-publishing-generate-published-provider-estate-csv")]
        public static async Task RunGeneratePublishedProviderEstateCsv([QueueTrigger(ServiceBusConstants.QueueNames.GeneratePublishedProviderEstateCsv,
            Connection = "AzureConnectionString")] string item, ILogger log)
        {
            using IServiceScope scope = Functions.Publishing.Startup.RegisterComponents(new ServiceCollection()).CreateScope();
            Message message = Helpers.ConvertToMessage<string>(item);

            OnGeneratePublishedProviderEstateCsv function = scope.ServiceProvider.GetService<OnGeneratePublishedProviderEstateCsv>();

            await function.Run(message);

            log.LogInformation($"C# Queue trigger function processed: {item}");
        }

        [FunctionName("on-publishing-generate-published-provider-estate-csv-failure")]
        public static async Task RunGeneratePublishedProviderEstateCsvFailure([QueueTrigger(ServiceBusConstants.QueueNames.GeneratePublishedProviderEstateCsvPoisonedLocal,
            Connection = "AzureConnectionString")] string item, ILogger log)
        {
            using IServiceScope scope = Functions.Publishing.Startup.RegisterComponents(new ServiceCollection()).CreateScope();
            Message message = Helpers.ConvertToMessage<string>(item);

            OnGeneratePublishedFundingCsvFailure function = scope.ServiceProvider.GetService<OnGeneratePublishedFundingCsvFailure>();

            await function.Run(message);

            log.LogInformation($"C# Queue trigger function processed: {item}");
        }
    }
}
