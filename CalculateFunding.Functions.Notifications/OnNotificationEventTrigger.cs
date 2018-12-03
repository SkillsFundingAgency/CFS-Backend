using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using Microsoft.Extensions.Logging;

namespace CalculateFunding.Functions.Notifications
{
    public static class OnNotificationEventTrigger
    {
        // Read from notification-events topic and send SignalR messages to clients
        [FunctionName("notification-event")]
        public static Task Run([ServiceBusTrigger("mytopic", "mysubscription", Connection = "")]Message message,
            [SignalR(HubName = "simplechat")] IAsyncCollector<SignalRMessage> signalRMessages,
            ILogger log)
        {
            // Send message body (JobNotification) to SignalR as body

            string messageText = "my wonderful content";

            return signalRMessages.AddAsync(
                new SignalRMessage
                {
                    Target = "newMessage",

                    Arguments = new[] { messageText }
                });
        }
    }
}
