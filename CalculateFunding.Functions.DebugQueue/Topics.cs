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
                logger.LogError("Error while executing Calcs OnEditSpecificationEvent", ex);
            }

            try
            {
                await Functions.TestEngine.ServiceBus.OnEditSpecificationEvent.Run(message);
            }
            catch (Exception ex)
            {
                logger.LogError("Error while executing TestEngine OnEditSpecificationEvent", ex);
            }

            try
            {
                await Functions.Users.ServiceBus.OnEditSpecificationEvent.Run(message);
            }
            catch (Exception ex)
            {
                logger.LogError("Error while executing Users OnEditSpecificationEvent", ex);
            }

            logger.LogInformation($"C# Queue trigger function processed: {item}");
        }

        [FunctionName("on-job-completion")]
        public static async Task OnJobCompletion(
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
                    await Jobs.ServiceBus.OnJobCompletion.Run(message);
                }
            }
            catch (Exception ex)
            {
                logger.LogError("Error while executing Jobs Completion Event", ex);
            }

            try
            {
                await Notifications.OnNotificationEventTrigger.Run(message, signalRMessages, logger);
            }
            catch (Exception ex)
            {
                logger.LogError("Error while executing Notification Event", ex);
            }

            logger.LogInformation($"C# Queue trigger function processed: {item}");
        }
    }
}
