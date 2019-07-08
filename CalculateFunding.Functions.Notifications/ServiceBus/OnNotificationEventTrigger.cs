using System;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Interfaces.Logging;
using CalculateFunding.Services.Notifications.Interfaces;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using Serilog;

namespace CalculateFunding.Functions.Notifications
{
    public class OnNotificationEventTrigger
    {
        private readonly ILogger _logger;
        private readonly INotificationService _notificationService;
        private readonly ICorrelationIdProvider _correlationIdProvider;

        public OnNotificationEventTrigger(
            ILogger logger,
            INotificationService notificationService,
            ICorrelationIdProvider correlationIdProvider)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(notificationService, nameof(notificationService));
            Guard.ArgumentNotNull(correlationIdProvider, nameof(correlationIdProvider));

            _logger = logger;
            _notificationService = notificationService;
            _correlationIdProvider = correlationIdProvider;
        }

        // Read from notification-events topic and send SignalR messages to clients
        [FunctionName("notification-event")]
        public async Task Run([ServiceBusTrigger(
                ServiceBusConstants.TopicNames.JobNotifications,
                ServiceBusConstants.TopicSubscribers.JobNotificationsToSignalR,
                Connection = ServiceBusConstants.ConnectionStringConfigurationKey)]Message message,
            [SignalR(HubName = JobConstants.NotificationsHubName)] IAsyncCollector<SignalRMessage> signalRMessages)
        {
            // Send message body (JobNotification) to SignalR as body
            Guard.ArgumentNotNull(message, nameof(message));
            Guard.ArgumentNotNull(signalRMessages, nameof(signalRMessages));

            try
            {
                _correlationIdProvider.SetCorrelationId(message.GetCorrelationId());
                await _notificationService.OnNotificationEvent(message, signalRMessages);
            }
            catch (Exception exception)
            {
                _logger.Error(exception, $"An error occurred getting message from topic: {ServiceBusConstants.TopicNames.JobNotifications} in subscriber { ServiceBusConstants.TopicSubscribers.JobNotificationsToSignalR}");
                throw;
            }
        }
    }
}
