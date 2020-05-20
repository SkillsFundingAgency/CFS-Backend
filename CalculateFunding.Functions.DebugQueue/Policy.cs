using System.Threading.Tasks;
using CalculateFunding.Functions.Policy.ServiceBus;
using CalculateFunding.Services.Core.Constants;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CalculateFunding.Functions.DebugQueue
{
    public static class Policy
    {
        [FunctionName("on-policy-reindex-templates")]
        public static async Task RunReIndexTemplate([QueueTrigger(ServiceBusConstants.QueueNames.PolicyReIndexTemplates,
            Connection = "AzureConnectionString")] string item, ILogger log)
        {
            using IServiceScope scope = Functions.Policy.Startup.RegisterComponents(new ServiceCollection()).CreateScope();
            Message message = Helpers.ConvertToMessage<string>(item);

            OnReIndexTemplates function = scope.ServiceProvider.GetService<OnReIndexTemplates>();

            await function.Run(message);

            log.LogInformation($"C# Queue trigger function processed: {item}");
        }
    }
}
