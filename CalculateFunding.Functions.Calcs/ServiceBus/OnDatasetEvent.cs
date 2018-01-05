using System.Threading.Tasks;
using CalculateFunding.Models;
using CalculateFunding.Models.Datasets;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace CalculateFunding.Functions.Calcs.ServiceBus
{
    public static class OnDatasetEvent
    {
        [FunctionName("on-dataset-event")]
        public static async Task Run(
            [ServiceBusTrigger("dataset-events", "dataset-events-calcs", Connection = "ServiceBusConnectionString")]
            string messageJson,
            ILogger logger)
        {
            var command = JsonConvert.DeserializeObject<Command>(messageJson);
            switch (command.TargetDocumentType)
            {
                case "Provider":
                    await OnProviderMessage(JsonConvert.DeserializeObject<ProviderCommand>(messageJson));
                    break;
            }
         }

        private static async Task OnProviderMessage(ProviderCommand command)
        {
            
        }
    }
}
