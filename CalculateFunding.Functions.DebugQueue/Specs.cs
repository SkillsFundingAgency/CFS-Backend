using System.Threading.Tasks;
using CalculateFunding.Models.Specs.Messages;
using CalculateFunding.Services.Core.Constants;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace CalculateFunding.Functions.DebugQueue
{
    public static class Specs
    {
        [FunctionName("on-add-relationship-event")]
        public static async Task Run([QueueTrigger(ServiceBusConstants.QueueNames.AddDefinitionRelationshipToSpecification, Connection = "AzureConnectionString")]string item, ILogger log)
        {
            Message message = Helpers.ConvertToMessage<AssignDefinitionRelationshipMessage>(item);

            await Functions.Specs.EventHub.OnAddRelatioshipEvent.Run(message);

            log.LogInformation($"C# Queue trigger function processed: {item}");
        }
    }
}
