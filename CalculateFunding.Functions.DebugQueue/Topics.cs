using System.Threading.Tasks;
using CalculateFunding.Services.Core.Constants;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;

namespace CalculateFunding.Functions.DebugQueue
{
    public static class Topics
    {
        [FunctionName("on-edit-specification")]
        public static async Task RunOnEditSpecificationEvent([QueueTrigger(ServiceBusConstants.TopicNames.EditSpecification, Connection = "AzureConnectionString")] string item, TraceWriter log)
        {
            Message message = Helpers.ConvertToMessage<Models.Specs.SpecificationVersionComparisonModel>(item);

            await Functions.Calcs.ServiceBus.OnEditSpecificationEvent.Run(message);
            await Functions.TestEngine.ServiceBus.OnEditSpecificationEvent.Run(message);
            await Functions.Users.ServiceBus.OnEditSpecificationEvent.Run(message);


            log.Info($"C# Queue trigger function processed: {item}");
        }
    }
}
