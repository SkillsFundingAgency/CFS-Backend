using System.Threading.Tasks;
using CalculateFunding.Functions.Specs.ServiceBus;
using CalculateFunding.Models.Messages;
using CalculateFunding.Services.Core.Constants;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CalculateFunding.Functions.DebugQueue
{
    public static class Specs
    {
        [FunctionName("on-add-relationship-event")]
        public static async Task RunAddRelationship([QueueTrigger(ServiceBusConstants.QueueNames.AddDefinitionRelationshipToSpecification, Connection = "AzureConnectionString")]string item, ILogger log)
        {
            using IServiceScope scope = Functions.Specs.Startup.RegisterComponents(new ServiceCollection()).CreateScope();
            
            Message message = Helpers.ConvertToMessage<AssignDefinitionRelationshipMessage>(item);

            OnAddRelationshipEvent function = scope.ServiceProvider.GetService<OnAddRelationshipEvent>();

            await function.Run(message);

            log.LogInformation($"C# Queue trigger function processed: {item}");
        }
        
        [FunctionName("on-reindex-specification")]
        public static async Task RunReIndexSpec([QueueTrigger(ServiceBusConstants.QueueNames.ReIndexSingleSpecification, Connection = "AzureConnectionString")]string item, ILogger log)
        {
            using IServiceScope scope = Functions.Specs.Startup.RegisterComponents(new ServiceCollection()).CreateScope();
            
            Message message = Helpers.ConvertToMessage<Message>(item);

            OnReIndexSpecification function = scope.ServiceProvider.GetService<OnReIndexSpecification>();

            await function.Run(message);

            log.LogInformation($"C# Queue trigger function processed: {item}");
        }

        [FunctionName("on-delete-specifications")]
        public static async Task RunDeleteSpecs([QueueTrigger(ServiceBusConstants.QueueNames.DeleteSpecifications, Connection = "AzureConnectionString")]string item, ILogger log)
        {
            using IServiceScope scope = Functions.Specs.Startup.RegisterComponents(new ServiceCollection()).CreateScope();

            Message message = Helpers.ConvertToMessage<string>(item);

            OnDeleteSpecifications function = scope.ServiceProvider.GetService<OnDeleteSpecifications>();

            await function.Run(message);

            log.LogInformation($"C# Queue trigger function processed: {item}");
        }
    }
}
