using System;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Jobs;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Notifications.Interfaces;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;

namespace CalculateFunding.Services.Notifications
{
    public class NotificationService : INotificationService
    {
        public async Task OnNotificationEvent(Message message, IAsyncCollector<SignalRMessage> signalRMessages)
        {
            Guard.ArgumentNotNull(message, nameof(message));
            Guard.ArgumentNotNull(signalRMessages, nameof(signalRMessages));

            JobNotification jobNotification = message.GetPayloadAsInstanceOf<JobNotification>();

            if (jobNotification == null)
            {
                throw new InvalidOperationException("Job notificiation was null");
            }

            // Send to all notifications channel
            await signalRMessages.AddAsync(
                    new SignalRMessage
                    {
                        Target = JobConstants.NotificationsTargetFunction,
                        GroupName = JobConstants.NotificationChannels.All,
                        Arguments = new[] { jobNotification }
                    });

            if (!string.IsNullOrWhiteSpace(jobNotification.SpecificationId))
            {
                // Send to individual specifications group
                await signalRMessages.AddAsync(
                    new SignalRMessage
                    {
                        Target = JobConstants.NotificationsTargetFunction,
                        GroupName = $"{JobConstants.NotificationChannels.SpecificationPrefix}{jobNotification.SpecificationId}",
                        Arguments = new[] { jobNotification }
                    });
            }

            if (string.IsNullOrWhiteSpace(jobNotification.ParentJobId))
            {
                // Send to parent jobs only group
                await signalRMessages.AddAsync(
                    new SignalRMessage
                    {
                        Target = JobConstants.NotificationsTargetFunction,
                        GroupName = JobConstants.NotificationChannels.ParentJobs,
                        Arguments = new[] { jobNotification }
                    });
            }
        }
    }
}
