using System.Threading.Tasks;
using CalculateFunding.Functions.Calcs.ServiceBus;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Services.Core.Constants;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CalculateFunding.Functions.DebugQueue
{
    public static class Calcs
    {
        [FunctionName("on-update-code-context-cache-poisoned")]
        public static async Task RunUpdateCodeContextCacheFailure([QueueTrigger(ServiceBusConstants.QueueNames.UpdateCodeContextCachePoisonedLocal, Connection = "AzureConnectionString")] string item, ILogger log)
        {
            using IServiceScope scope = Functions.Calcs.Startup.RegisterComponents(new ServiceCollection()).CreateScope();
            Message message = Helpers.ConvertToMessage<DatasetRelationshipSummary>(item);

            OnUpdateCodeContextCacheFailure function = scope.ServiceProvider.GetService<OnUpdateCodeContextCacheFailure>();

            await function.Run(message);

            log.LogInformation($"C# Queue trigger function processed: {item}");
        }

        [FunctionName("on-update-code-context-cache")]
        public static async Task RunUpdateCodeContextCache([QueueTrigger(ServiceBusConstants.QueueNames.UpdateCodeContextCache, Connection = "AzureConnectionString")] string item, ILogger log)
        {
            using IServiceScope scope = Functions.Calcs.Startup.RegisterComponents(new ServiceCollection()).CreateScope();
            Message message = Helpers.ConvertToMessage<string>(item);

            OnUpdateCodeContextCache function = scope.ServiceProvider.GetService<OnUpdateCodeContextCache>();

            await function.Run(message);

            log.LogInformation($"C# Queue trigger function processed: {item}");
        }
        
        [FunctionName("on-calcs-add-data-relationship")]
        public static async Task RunCalcsAddRelationshipToBuildProject([QueueTrigger(ServiceBusConstants.QueueNames.UpdateBuildProjectRelationships, Connection = "AzureConnectionString")] string item, ILogger log)
        {
            using IServiceScope scope = Functions.Calcs.Startup.RegisterComponents(new ServiceCollection()).CreateScope();
            Message message = Helpers.ConvertToMessage<DatasetRelationshipSummary>(item);

            CalcsAddRelationshipToBuildProject function = scope.ServiceProvider.GetService<CalcsAddRelationshipToBuildProject>();

            await function.Run(message);

            log.LogInformation($"C# Queue trigger function processed: {item}");
        }

        [FunctionName("on-calcs-instruct-allocations")]
        public static async Task RunOnCalcsInstructAllocationResults([QueueTrigger(ServiceBusConstants.QueueNames.CalculationJobInitialiser, Connection = "AzureConnectionString")] string item, ILogger log)
        {
            using IServiceScope scope = Functions.Calcs.Startup.RegisterComponents(new ServiceCollection()).CreateScope();
            Message message = Helpers.ConvertToMessage<Dataset>(item);

            OnCalcsInstructAllocationResults function = scope.ServiceProvider.GetService<OnCalcsInstructAllocationResults>();

            await function.Run(message);

            log.LogInformation($"C# Queue trigger function processed: {item}");
        }

        [FunctionName("on-delete-calculations")]
        public static async Task RunOnDeleteCalculations([QueueTrigger(ServiceBusConstants.QueueNames.DeleteCalculations, Connection = "AzureConnectionString")] string item, ILogger log)
        {
            using IServiceScope scope = Functions.Calcs.Startup.RegisterComponents(new ServiceCollection()).CreateScope();
            Message message = Helpers.ConvertToMessage<string>(item);

            OnDeleteCalculations function = scope.ServiceProvider.GetService<OnDeleteCalculations>();

            await function.Run(message);

            log.LogInformation($"C# Queue trigger function processed: {item}");
        }

        [FunctionName("on-calcs-instruct-allocations-poisoned")]
        public static async Task RunOnCalcsInstructAllocationResultsFailure([QueueTrigger(ServiceBusConstants.QueueNames.CalculationJobInitialiserPoisonedLocal, Connection = "AzureConnectionString")] string item, ILogger log)
        {
            using IServiceScope scope = Functions.Calcs.Startup.RegisterComponents(new ServiceCollection()).CreateScope();
            Message message = Helpers.ConvertToMessage<string>(item);

            OnCalcsInstructAllocationResultsFailure function = scope.ServiceProvider.GetService<OnCalcsInstructAllocationResultsFailure>();

            await function.Run(message);

            log.LogInformation($"C# Queue trigger function processed: {item}");
        }

        [FunctionName("on-apply-template-calculations")]
        public static async Task RunOnApplyTemplateCalculations([QueueTrigger(ServiceBusConstants.QueueNames.ApplyTemplateCalculations, Connection = "AzureConnectionString")] string item, ILogger log)
        {
            using IServiceScope scope = Functions.Calcs.Startup.RegisterComponents(new ServiceCollection()).CreateScope();
            Message message = Helpers.ConvertToMessage<string>(item);

            OnApplyTemplateCalculations function = scope.ServiceProvider.GetService<OnApplyTemplateCalculations>();

            await function.Run(message);

            log.LogInformation($"C# Queue trigger function processed: {item}");
        }

        [FunctionName("on-apply-template-calculations-poisoned")]
        public static async Task RunOnApplyTemplateCalculationsFailure([QueueTrigger(ServiceBusConstants.QueueNames.ApplyTemplateCalculationsPoisonedLocal, Connection = "AzureConnectionString")] string item, ILogger log)
        {
            using IServiceScope scope = Functions.Calcs.Startup.RegisterComponents(new ServiceCollection()).CreateScope();
            Message message = Helpers.ConvertToMessage<string>(item);

            OnApplyTemplateCalculationsFailure function = scope.ServiceProvider.GetService<OnApplyTemplateCalculationsFailure>();

            await function.Run(message);

            log.LogInformation($"C# Queue trigger function processed: {item}");
        }

        [FunctionName("on-reindex-specification-calculation-relationships")]
        public static async Task RunOnReIndexSpecificationCalculationRelationships([QueueTrigger(ServiceBusConstants.QueueNames.ReIndexSpecificationCalculationRelationships, Connection = "AzureConnectionString")] string item, ILogger log)
        {
            using IServiceScope scope = Functions.Calcs.Startup.RegisterComponents(new ServiceCollection()).CreateScope();
            Message message = Helpers.ConvertToMessage<string>(item);

            OnReIndexSpecificationCalculationRelationships function = scope.ServiceProvider.GetService<OnReIndexSpecificationCalculationRelationships>();

            await function.Run(message);

            log.LogInformation($"C# Queue trigger function processed: {item}");
        }

        [FunctionName("on-reindex-specification-calculation-relationships-poisoned")]
        public static async Task RunOnReIndexSpecificationCalculationRelationshipsFailure([QueueTrigger(ServiceBusConstants.QueueNames.ReIndexSpecificationCalculationRelationshipsPoisonedLocal, Connection = "AzureConnectionString")] string item, ILogger log)
        {
            using IServiceScope scope = Functions.Calcs.Startup.RegisterComponents(new ServiceCollection()).CreateScope();
            Message message = Helpers.ConvertToMessage<string>(item);

            OnReIndexSpecificationCalculationRelationshipsFailure function = scope.ServiceProvider.GetService<OnReIndexSpecificationCalculationRelationshipsFailure>();

            await function.Run(message);

            log.LogInformation($"C# Queue trigger function processed: {item}");
        }

        [FunctionName("on-approve-all-calculations")]
        public static async Task RunOnApproveAllCalculations(
            [QueueTrigger(ServiceBusConstants.QueueNames.ApproveAllCalculations, Connection = "AzureConnectionString")] string item, ILogger log)
        {
            using IServiceScope scope = Functions.Calcs.Startup.RegisterComponents(new ServiceCollection()).CreateScope();
            Message message = Helpers.ConvertToMessage<string>(item);

            OnApproveAllCalculations function = scope.ServiceProvider.GetService<OnApproveAllCalculations>();

            await function.Run(message);

            log.LogInformation($"C# Queue trigger function processed: {item}");
        }

        [FunctionName("on-approve-all-calculations-poisoned")]
        public static async Task RunOnApproveAllCalculationsFailure(
            [QueueTrigger(ServiceBusConstants.QueueNames.ApproveAllCalculationsPoisonedLocal, Connection = "AzureConnectionString")] string item, ILogger log)
        {
            using IServiceScope scope = Functions.Calcs.Startup.RegisterComponents(new ServiceCollection()).CreateScope();
            Message message = Helpers.ConvertToMessage<string>(item);

            OnApproveAllCalculationsFailure function = scope.ServiceProvider.GetService<OnApproveAllCalculationsFailure>();

            await function.Run(message);

            log.LogInformation($"C# Queue trigger function processed: {item}");
        }

        [FunctionName("on-referenced-specification-remap")]
        public static async Task RunOnReferencedSpecificationReMap(
            [QueueTrigger(ServiceBusConstants.QueueNames.ReferencedSpecificationReMap, Connection = "AzureConnectionString")] string item, ILogger log)
        {
            using IServiceScope scope = Functions.Calcs.Startup.RegisterComponents(new ServiceCollection()).CreateScope();
            Message message = Helpers.ConvertToMessage<string>(item);

            OnReferencedSpecificationReMap function = scope.ServiceProvider.GetService<OnReferencedSpecificationReMap>();

            await function.Run(message);

            log.LogInformation($"C# Queue trigger function processed: {item}");
        }

        [FunctionName("on-referenced-specification-remap-poisoned")]
        public static async Task RunOnReferencedSpecificationReMapFailure(
            [QueueTrigger(ServiceBusConstants.QueueNames.ReferencedSpecificationReMapPoisonedLocal, Connection = "AzureConnectionString")] string item, ILogger log)
        {
            using IServiceScope scope = Functions.Calcs.Startup.RegisterComponents(new ServiceCollection()).CreateScope();
            Message message = Helpers.ConvertToMessage<string>(item);

            OnReferencedSpecificationReMapFailure function = scope.ServiceProvider.GetService<OnReferencedSpecificationReMapFailure>();

            await function.Run(message);

            log.LogInformation($"C# Queue trigger function processed: {item}");
        }
    }
}
