using CalculateFunding.Models.Results;
using CalculateFunding.Services.Core.Constants;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using System.Threading.Tasks;

namespace CalculateFunding.Functions.DebugQueue
{
    public static class Results
    {
        [FunctionName("on-provider-results-published")]
        public static async Task RunPublishProviderResults([QueueTrigger(ServiceBusConstants.QueueNames.PublishProviderResults, Connection = "AzureConnectionString")] string item, TraceWriter log)
        {
            Message message = Helpers.ConvertToMessage<string>(item);

            await Functions.Results.ServiceBus.OnProviderResultsPublishedEvent.Run(message);

            log.Info($"C# Queue trigger function processed: {item}");
        }

        [FunctionName("on-fetch-provider-profile")]
        public static async Task RunFetchProviderProfile([QueueTrigger(ServiceBusConstants.QueueNames.FetchProviderProfile, Connection = "AzureConnectionString")] string item, TraceWriter log)
        {
            Message message = Helpers.ConvertToMessage<ProviderProfilingRequestModel>(item);

            await Functions.Results.ServiceBus.OnFetchProviderProfileEvent.Run(message);

            log.Info($"C# Queue trigger function processed: {item}");
        }
    }
}
