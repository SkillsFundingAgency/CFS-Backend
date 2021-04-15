using CalculateFunding.Functions.Users.ServiceBus;
using CalculateFunding.Services.Core.Constants;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace CalculateFunding.Functions.DebugQueue
{
    public static class Users
    {
        [FunctionName("users-on-reindex-users")]
        public static async Task RunReIndexUser([QueueTrigger(ServiceBusConstants.QueueNames.UsersReIndexUsers, Connection = "AzureConnectionString")]string item, ILogger log)
        {
            using IServiceScope scope = Functions.Users.Startup.RegisterComponents(new ServiceCollection()).CreateScope();

            Message message = Helpers.ConvertToMessage<Message>(item);

            OnReIndexUsersEvent function = scope.ServiceProvider.GetService<OnReIndexUsersEvent>();

            await function.Run(message);

            log.LogInformation($"C# Queue trigger function processed: {item}");
        }
    }
}
