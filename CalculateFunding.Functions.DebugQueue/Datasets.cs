using System.Globalization;
using System.Threading.Tasks;
using CalculateFunding.Functions.Datasets.ServiceBus;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Services.Core.Constants;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CalculateFunding.Functions.DebugQueue
{
    public static class Datasets
    {
        [FunctionName("on-dataset-event")]
        public static async Task RunPublishProviderResults([QueueTrigger(ServiceBusConstants.QueueNames.ProcessDataset, Connection = "AzureConnectionString")] string item, ILogger log)
        {
            using (IServiceScope scope = Functions.Datasets.Startup.RegisterComponents(new ServiceCollection()).CreateScope())
            {
                CultureInfo.CurrentCulture = new CultureInfo("en-GB");
                Message message = Helpers.ConvertToMessage<Dataset>(item);

                OnDatasetEvent function = scope.ServiceProvider.GetService<OnDatasetEvent>();

                await function.Run(message);

                log.LogInformation($"C# Queue trigger function processed: {item}");
            }
        }

        [FunctionName("on-dataset-validation-event")]
        public static async Task RunValidateDatasetEvent([QueueTrigger(ServiceBusConstants.QueueNames.ValidateDataset, Connection = "AzureConnectionString")] string item, ILogger log)
        {
            using (IServiceScope scope = Functions.Datasets.Startup.RegisterComponents(new ServiceCollection()).CreateScope())
            {
                CultureInfo.CurrentCulture = new CultureInfo("en-GB");
                Message message = Helpers.ConvertToMessage<GetDatasetBlobModel>(item);

                OnDatasetValidationEvent function = scope.ServiceProvider.GetService<OnDatasetValidationEvent>();

                await function.Run(message);

                log.LogInformation($"C# Queue trigger function processed: {item}");
            }
        }
    }
}
