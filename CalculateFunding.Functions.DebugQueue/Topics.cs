using System;
using System.Threading.Tasks;
using CalculateFunding.Models.Jobs;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Extensions;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using Microsoft.Extensions.Logging;

namespace CalculateFunding.Functions.DebugQueue
{
    public static class Topics
    {
        [FunctionName("on-edit-specification")]
        public static async Task RunOnEditSpecificationEvent([QueueTrigger(ServiceBusConstants.TopicNames.EditSpecification, Connection = "AzureConnectionString")] string item, ILogger logger)
        {
            Message message = Helpers.ConvertToMessage<Models.Specs.SpecificationVersionComparisonModel>(item);

            try
            {
                await Functions.Calcs.ServiceBus.OnEditSpecificationEvent.Run(message);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error while executing Calcs OnEditSpecificationEvent");
            }

            try
            {
                await Functions.TestEngine.ServiceBus.OnEditSpecificationEvent.Run(message);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error while executing TestEngine OnEditSpecificationEvent");
            }

            try
            {
                await Functions.Users.ServiceBus.OnEditSpecificationEvent.Run(message);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error while executing Users OnEditSpecificationEvent");
            }

            logger.LogInformation($"C# Queue trigger function processed: {item}");
        }

        [FunctionName("on-job-notification")]
        public static async Task OnJobNotification(
            [QueueTrigger(ServiceBusConstants.TopicNames.JobNotifications, Connection = "AzureConnectionString")] string item,
            [SignalR(HubName = JobConstants.NotificationsHubName)] IAsyncCollector<SignalRMessage> signalRMessages,
            ILogger logger)
        {
            Message message = Helpers.ConvertToMessage<JobNotification>(item);

            JobNotification jobNotification = message.GetPayloadAsInstanceOf<JobNotification>();
            try
            {
                if (jobNotification.CompletionStatus == CompletionStatus.Succeeded && jobNotification.JobType == JobConstants.DefinitionNames.CreateInstructGenerateAggregationsAllocationJob)
                {
                    await Functions.Calcs.ServiceBus.OnCalculationAggregationsJobCompleted.Run(message);
                }
                else
                {
                    await Jobs.ServiceBus.OnJobNotification.Run(message);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error while executing Jobs Notification Event");
            }

            try
            {
                await Notifications.OnNotificationEventTrigger.Run(message, signalRMessages);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error while executing Notification Event");
            }

            logger.LogInformation($"C# Queue trigger function processed: {item}");
        }
    }
}
