using System.Threading.Tasks;
using CalculateFunding.Models.Specs.Messages;
using CalculateFunding.Services.Core.Constants;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;

namespace CalculateFunding.Functions.DebugQueue
{
    public static class Specs
    {
        [FunctionName("on-add-relationship-event")]
        public static async Task Run([QueueTrigger(ServiceBusConstants.QueueNames.AddDefinitionRelationshipToSpecification, Connection = "AzureConnectionString")]string item, TraceWriter log)
        {
            Message message = Helpers.ConvertToMessage<AssignDefinitionRelationshipMessage>(item);

            await Functions.Specs.EventHub.OnAddRelatioshipEvent.Run(message);

            log.Info($"C# Queue trigger function processed: {item}");
        }
    }
}
