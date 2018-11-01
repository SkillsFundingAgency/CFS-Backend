using System.Threading.Tasks;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Services.Core.Constants;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;

namespace CalculateFunding.Functions.DebugQueue
{
    public static class Calcs
    {
        [FunctionName("on-calc-events-create-draft")]
        public static async Task RunOnCalcsCreateDraftEvent([QueueTrigger(ServiceBusConstants.QueueNames.CreateDraftCalculation, Connection = "AzureConnectionString")] string item, TraceWriter log)
        {
            Message message = Helpers.ConvertToMessage<Calculation>(item);

            await Functions.Calcs.ServiceBus.OnCalcsCreateDraftEvent.Run(message);

            log.Info($"C# Queue trigger function processed: {item}");
        }

        [FunctionName("on-calcs-add-data-relationship")]
        public static async Task RunCalcsAddRelationshipToBuildProject([QueueTrigger(ServiceBusConstants.QueueNames.UpdateBuildProjectRelationships, Connection = "AzureConnectionString")] string item, TraceWriter log)
        {
            Message message = Helpers.ConvertToMessage<DatasetRelationshipSummary>(item);

            await Functions.Calcs.ServiceBus.CalcsAddRelationshipToBuildProject.Run(message);

            log.Info($"C# Queue trigger function processed: {item}");
        }

        [FunctionName("on-calcs-instruct-allocations")]
        public static async Task RunOnCalcsInstructAllocationResults([QueueTrigger(ServiceBusConstants.QueueNames.CalculationJobInitialiser, Connection = "AzureConnectionString")] string item, TraceWriter log)
        {
            Message message = Helpers.ConvertToMessage<string>(item);

            await Functions.Calcs.ServiceBus.OnCalcsInstructAllocationResults.Run(message);

            log.Info($"C# Queue trigger function processed: {item}");
        }

        [FunctionName("on-edit-calculation-for-calcs")]
        public static async Task RunOnEditCalculationSpecificationEvent([QueueTrigger(ServiceBusConstants.TopicNames.EditCalculation, Connection = "AzureConnectionString")] string item, TraceWriter log)
        {
            Message message = Helpers.ConvertToMessage<Models.Specs.CalculationVersionComparisonModel>(item);

            await Functions.Calcs.ServiceBus.OnEditCalculationSpecificationEvent.Run(message);

            log.Info($"C# Queue trigger function processed: {item}");
        }
    }
}
