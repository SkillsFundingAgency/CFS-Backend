using System;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Functions;
using CalculateFunding.Services.Core.Interfaces.ServiceBus;
using CalculateFunding.Services.Notifications.Interfaces;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using Serilog;

namespace CalculateFunding.Functions.Notifications
{
    public class OnNotificationEventTrigger : SmokeTest
    {
        private readonly ILogger _logger;
        private readonly INotificationService _notificationService;
        public const string FunctionName = "notification-event";

        public OnNotificationEventTrigger(
            ILogger logger,
            INotificationService notificationService,
            IMessengerService messegerService,
            bool isDevelopment = false) : base(logger, messegerService, FunctionName, isDevelopment)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(notificationService, nameof(notificationService));

            _logger = logger;
            _notificationService = notificationService;
        }

        // Read from notification-events topic and send SignalR messages to clients
        [FunctionName(FunctionName)]
        public async Task Run([ServiceBusTrigger(
                ServiceBusConstants.TopicNames.JobNotifications,
                ServiceBusConstants.TopicSubscribers.JobNotificationsToSignalR,
                Connection = ServiceBusConstants.ConnectionStringConfigurationKey)]Message message,
            [SignalR(HubName = JobConstants.NotificationsHubName)] IAsyncCollector<SignalRMessage> signalRMessages)
        {
            // Send message body (JobNotification) to SignalR as body
            Guard.ArgumentNotNull(message, nameof(message));

            await Run(async () =>
            {
                Guard.ArgumentNotNull(signalRMessages, nameof(signalRMessages));
                try
                {
                    await _notificationService.OnNotificationEvent(message, signalRMessages);
                }
                catch (Exception exception)
                {
                    _logger.Error(exception, $"An error occurred getting message from topic: {ServiceBusConstants.TopicNames.JobNotifications} in subscriber { ServiceBusConstants.TopicSubscribers.JobNotificationsToSignalR}");
                    throw;
                }
            },
            message);
        }
    }
}
