using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Jobs;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Interfaces.ServiceBus;
using CalculateFunding.Services.Jobs.Interfaces;
using Polly;

namespace CalculateFunding.Services.Jobs
{
    public class NotificationService : INotificationService
    {
        private readonly IMessengerService _messengerService;
        private readonly Policy _messengerServicePolicy;

        public NotificationService(IMessengerService messengerService, IJobsResiliencePolicies resiliencePolicies)
        {
            Guard.ArgumentNotNull(messengerService, nameof(messengerService));
            Guard.ArgumentNotNull(resiliencePolicies, nameof(resiliencePolicies));

            _messengerService = messengerService;
            _messengerServicePolicy = resiliencePolicies.MessengerServicePolicy;
        }

        public async Task SendNotification(JobNotification jobNotification)
        {
            Guard.ArgumentNotNull(jobNotification, nameof(jobNotification));
            Guard.ArgumentNotNull(jobNotification.Trigger, nameof(jobNotification.Trigger));

            Guard.IsNullOrWhiteSpace(jobNotification.Trigger.EntityId, nameof(jobNotification.Trigger.EntityId));
            Guard.IsNullOrWhiteSpace(jobNotification.JobId, nameof(jobNotification.JobId));
            Guard.IsNullOrWhiteSpace(jobNotification.JobType, nameof(jobNotification.JobType));

            // Use properties so the topic can be filtered by consumers based on these fields
            Dictionary<string, string> properties = new Dictionary<string, string>
            {
                { "specificationId", jobNotification.SpecificationId },
                { "entityId", jobNotification.Trigger.EntityId },
                { "jobType", jobNotification.JobType },
                { "jobId", jobNotification.JobId }
            };

            await _messengerServicePolicy.ExecuteAsync(() => _messengerService.SendToTopic(ServiceBusConstants.TopicNames.JobNotifications, jobNotification, properties));
        }
    }
}
