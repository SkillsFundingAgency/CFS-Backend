using System;
using System.Threading.Tasks;
using CalculateFunding.Functions.Calcs.ServiceBus;
using CalculateFunding.Functions.CosmosDbScaling.ServiceBus;
using CalculateFunding.Functions.Results.ServiceBus;
using CalculateFunding.Functions.TestEngine.ServiceBus;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Models.Jobs;
using CalculateFunding.Models.Messages;
using CalculateFunding.Models.Scenarios;
using CalculateFunding.Models.Specs;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Extensions;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CalculateFunding.Functions.DebugQueue
{
    public static class Topics
    {
        [FunctionName("on-provider-sourcedataset-cleanup")]
        public static async Task RunOnProviderSourceDatasetCleanup([QueueTrigger(ServiceBusConstants.TopicNames.ProviderSourceDatasetCleanup, Connection = "AzureConnectionString")] string item, ILogger logger)
        {
            Message message = Helpers.ConvertToMessage<SpecificationProviders>(item);

            using (IServiceScope scope = Functions.Results.Startup.RegisterComponents(new ServiceCollection()).CreateScope())
            {
                try
                {
                    OnProviderResultsSpecificationCleanup function = scope.ServiceProvider.GetService<OnProviderResultsSpecificationCleanup>();

                    await function.Run(message);

                }
                catch (Exception ex)
                {
                    logger.LogError(ex, $"Error while executing Results {nameof(RunOnProviderSourceDatasetCleanup)}");
                }
            }

            using (IServiceScope scope = Functions.TestEngine.Startup.RegisterComponents(new ServiceCollection()).CreateScope())
            {
                try
                {
                    OnTestSpecificationProviderResultsCleanup function = scope.ServiceProvider.GetService<OnTestSpecificationProviderResultsCleanup>();

                    await function.Run(message);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, $"Error while executing TestEngine {nameof(RunOnProviderSourceDatasetCleanup)}");
                }
            }

            logger.LogInformation($"C# Queue trigger function processed: {item}");
        }

        [FunctionName("on-edit-specification")]
        public static async Task RunOnEditSpecificationEvent([QueueTrigger(ServiceBusConstants.TopicNames.EditSpecification, Connection = "AzureConnectionString")] string item, ILogger logger)
        {
            Message message = Helpers.ConvertToMessage<SpecificationVersionComparisonModel>(item);

            using (IServiceScope scope = Functions.TestEngine.Startup.RegisterComponents(new ServiceCollection()).CreateScope())
            {
                try
                {
                    Functions.TestEngine.ServiceBus.OnEditSpecificationEvent function = scope.ServiceProvider.GetService<Functions.TestEngine.ServiceBus.OnEditSpecificationEvent>();

                    await function.Run(message);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, $"Error while executing TestEngine {nameof(RunOnEditSpecificationEvent)}");
                }
            }

            using (IServiceScope scope = Functions.Users.Startup.RegisterComponents(new ServiceCollection()).CreateScope())
            {
                try
                {
                    Functions.Users.ServiceBus.OnEditSpecificationEvent function = scope.ServiceProvider.GetService<Functions.Users.ServiceBus.OnEditSpecificationEvent>();

                    await function.Run(message);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, $"Error while executing Users {nameof(RunOnEditSpecificationEvent)}");
                }
            }
            logger.LogInformation($"C# Queue trigger function processed: {item}");
        }


        [FunctionName("on-data-definition-changes")]
        public static async Task OnDataDefinitionChanges([QueueTrigger(ServiceBusConstants.TopicNames.DataDefinitionChanges, Connection = "AzureConnectionString")] string item, ILogger logger)
        {
            Message message = Helpers.ConvertToMessage<DatasetDefinitionChanges>(item);

            using (IServiceScope scope = Functions.Datasets.Startup.RegisterComponents(new ServiceCollection()).CreateScope())
            {
                try
                {
                    Functions.Datasets.ServiceBus.OnDataDefinitionChanges function = scope.ServiceProvider.GetService<Functions.Datasets.ServiceBus.OnDataDefinitionChanges>();

                    await function.Run(message);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, $"Error while executing Datasets {nameof(OnDataDefinitionChanges)}");
                }
            }

            using (IServiceScope scope = Functions.Scenarios.Startup.RegisterComponents(new ServiceCollection()).CreateScope())
            {
                try
                {
                    Functions.Scenarios.ServiceBus.OnDataDefinitionChanges function = scope.ServiceProvider.GetService<Functions.Scenarios.ServiceBus.OnDataDefinitionChanges>();

                    await function.Run(message);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, $"Error while executing Scenarios {nameof(OnDataDefinitionChanges)}");
                }
            }

            using (IServiceScope scope = Functions.Calcs.Startup.RegisterComponents(new ServiceCollection()).CreateScope())
            {
                try
                {
                    Functions.Calcs.ServiceBus.OnDataDefinitionChanges function = scope.ServiceProvider.GetService<Functions.Calcs.ServiceBus.OnDataDefinitionChanges>();

                    await function.Run(message);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, $"Error while executing Calcs {nameof(OnDataDefinitionChanges)}");
                }
            }

            logger.LogInformation($"C# Queue trigger function processed: {item}");
        }

        [FunctionName("on-job-notification")]
        public static async Task OnJobNotification(
            [QueueTrigger(ServiceBusConstants.TopicNames.JobNotifications, Connection = "AzureConnectionString")] string item,
            [SignalR(HubName = JobConstants.NotificationsHubName)] IAsyncCollector<SignalRMessage> signalRMessages,
            ILogger logger)
        {
            Message message = Helpers.ConvertToMessage<JobSummary>(item);

            JobSummary jobNotification = message.GetPayloadAsInstanceOf<JobSummary>();
           
                try
                {
                    if (jobNotification.CompletionStatus == CompletionStatus.Succeeded && jobNotification.JobType == JobConstants.DefinitionNames.CreateInstructGenerateAggregationsAllocationJob)
                    {
                    using IServiceScope scope = Functions.Calcs.Startup.RegisterComponents(new ServiceCollection()).CreateScope();
                    OnCalculationAggregationsJobCompleted function = scope.ServiceProvider.GetService<OnCalculationAggregationsJobCompleted>();

                    await function.Run(message);
                }
                    else
                    {
                    using IServiceScope scope = Jobs.Startup.RegisterComponents(new ServiceCollection()).CreateScope();
                    Jobs.ServiceBus.OnJobNotification function = scope.ServiceProvider.GetService<Jobs.ServiceBus.OnJobNotification>();

                    await function.Run(message);
                }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error while executing Jobs Notification Event");
                }
            

            using (IServiceScope scope = Functions.Notifications.Startup.RegisterComponents(new ServiceCollection()).CreateScope())
            {
                try
                {
                    Notifications.OnNotificationEventTrigger function = scope.ServiceProvider.GetService<Notifications.OnNotificationEventTrigger>();

                    await function.Run(message, signalRMessages);

                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error while executing Notification Event");
                }
            }

            using (IServiceScope scope = Functions.CosmosDbScaling.Startup.RegisterComponents(new ServiceCollection()).CreateScope())
            {
                try
                {
                    OnScaleUpCosmosDbCollection function = scope.ServiceProvider.GetService<OnScaleUpCosmosDbCollection>();

                    await function.Run(message);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error while executing Scale Up Event");
                }

                logger.LogInformation($"C# Queue trigger function processed: {item}");
            }
        }
    }
}
