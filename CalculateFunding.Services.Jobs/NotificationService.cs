using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Models.Jobs;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Interfaces.ServiceBus;
using CalculateFunding.Services.Jobs.Interfaces;

namespace CalculateFunding.Services.Jobs
{
    public class NotificationService : INotificationService
    {
        private readonly IMessengerService _messengerService;

        public NotificationService(IMessengerService messengerService)
        {
            _messengerService = messengerService;
        }

        public async Task SendNotification(JobNotification jobNotification)
        {
            // Use properties so the topic can be filtered by consumers based on these fields
            Dictionary<string, string> properties = new Dictionary<string, string>();
            properties.Add("specificationId", jobNotification.SpecificationId);
            properties.Add("entityId", jobNotification.Trigger.EntityId);
            properties.Add("jobType", jobNotification.JobType);
            properties.Add("jobId", jobNotification.JobId);

            await _messengerService.SendToTopic(ServiceBusConstants.TopicNames.JobNotifications, jobNotification, properties);
        }
    }
}
