using System;
using System.Threading.Tasks;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.ServiceBus.Interfaces;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Functions;
using CalculateFunding.Services.Notifications.Interfaces;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using Serilog;

namespace CalculateFunding.Functions.Notifications
{
    public class OnNotificationEventTrigger : Retriable
    {
        private readonly INotificationService _notificationService;
        public const string FunctionName = "notification-event";

        public OnNotificationEventTrigger(
            ILogger logger,
            INotificationService notificationService,
            IMessengerService messengerService,
            IUserProfileProvider userProfileProvider, bool useAzureStorage = false) 
            : base(logger, messengerService, FunctionName, $"{ServiceBusConstants.TopicNames.JobNotifications}/{ServiceBusConstants.TopicSubscribers.JobNotificationsToSignalR}", useAzureStorage, userProfileProvider, notificationService)
        {
            Guard.ArgumentNotNull(notificationService, nameof(notificationService));

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
            await base.Run(message,
            async () =>
            {
                await _notificationService.OnNotificationEvent(message, signalRMessages);
            });
        }
    }
}
