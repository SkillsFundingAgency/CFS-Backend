using System.Threading.Tasks;
using CalculateFunding.Functions.Publishing.ServiceBus;
using CalculateFunding.Services.Core.Constants;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CalculateFunding.Functions.DebugQueue
{
    public static class Publishing
    {
        [FunctionName("on-publishing-approve-funding")]
        public static async Task RunApproveFundingTests([QueueTrigger(ServiceBusConstants.QueueNames.PublishingApproveFunding, Connection = "AzureConnectionString")] string item, ILogger log)
        {
            using (IServiceScope scope = Functions.Publishing.Startup.RegisterComponents(new ServiceCollection()).CreateScope())
            {
                Message message = Helpers.ConvertToMessage<string>(item);

                OnApproveFunding function = scope.ServiceProvider.GetService<OnApproveFunding>();

                await function.Run(message);

                log.LogInformation($"C# Queue trigger function processed: {item}");
            }
        }

        [FunctionName("on-publishing-refresh-funding")]
        public static async Task RunRefreshFundingTests([QueueTrigger(ServiceBusConstants.QueueNames.PublishingRefreshFunding, Connection = "AzureConnectionString")] string item, ILogger log)
        {
            using (IServiceScope scope = Functions.Publishing.Startup.RegisterComponents(new ServiceCollection()).CreateScope())
            {
                Message message = Helpers.ConvertToMessage<string>(item);

                OnRefreshFunding function = scope.ServiceProvider.GetService<OnRefreshFunding>();

                await function.Run(message);

                log.LogInformation($"C# Queue trigger function processed: {item}");
            }
        }

        [FunctionName("on-publishing-publish-funding")]
        public static async Task RunPublishFundingTests([QueueTrigger(ServiceBusConstants.QueueNames.PublishingPublishFunding, Connection = "AzureConnectionString")] string item, ILogger log)
        {
            using (IServiceScope scope = Functions.Publishing.Startup.RegisterComponents(new ServiceCollection()).CreateScope())
            {
                Message message = Helpers.ConvertToMessage<string>(item);

                OnPublishFunding function = scope.ServiceProvider.GetService<OnPublishFunding>();

                await function.Run(message);

                log.LogInformation($"C# Queue trigger function processed: {item}");
            }
        }
    }
}
