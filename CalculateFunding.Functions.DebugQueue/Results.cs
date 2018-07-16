using CalculateFunding.Services.Core.Constants;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using System;
using System.Collections.Generic;
using System.Text;
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
    }
}
