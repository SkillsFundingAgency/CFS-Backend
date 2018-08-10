using System.Threading.Tasks;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Services.Core.Constants;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;

namespace CalculateFunding.Functions.DebugQueue
{
    public static class Datasets
    {
        [FunctionName("on-dataset-event")]
        public static async Task RunPublishProviderResults([QueueTrigger(ServiceBusConstants.QueueNames.ProcessDataset, Connection = "AzureConnectionString")] string item, TraceWriter log)
        {
            Message message = Helpers.ConvertToMessage<Dataset>(item);

            await Functions.Datasets.ServiceBus.OnDatasetEvent.Run(message);

            log.Info($"C# Queue trigger function processed: {item}");
        }
    }
}
