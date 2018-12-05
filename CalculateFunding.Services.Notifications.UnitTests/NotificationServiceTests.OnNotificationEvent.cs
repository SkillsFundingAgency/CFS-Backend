using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CalculateFunding.Models.Jobs;
using CalculateFunding.Services.Core.Constants;
using FluentAssertions;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using NSubstitute;

namespace CalculateFunding.Services.Notifications.UnitTests
{
    [TestClass]
    public partial class NotificationServiceTests
    {
        [TestMethod]
        public async Task OnNotificationEvent_WhenParentJobWithoutSpecificationIdIsCreated_ThenSignalRMessagesAdded()
        {
            // Arrange
            NotificationService service = CreateService();

            JobNotification jobNotification = new JobNotification()
            {
                CompletionStatus = null,
                JobId = JobId,
                JobType = "test",

            };

            string json = JsonConvert.SerializeObject(jobNotification);

            Message message = new Message(Encoding.UTF8.GetBytes(json));

            IAsyncCollector<SignalRMessage> generatedMessages = CreateSignalRMessageCollector();

            // Act
            await service.OnNotificationEvent(message, generatedMessages);

            // Assert
            await generatedMessages
                .Received(2)
                .AddAsync(Arg.Any<SignalRMessage>());

            await generatedMessages
                .Received(1)
                .AddAsync(
                Arg.Is<SignalRMessage>(
                    c => c.Target == JobConstants.NotificationsTargetFunction &&
                    c.Arguments.Length == 1 &&
                    c.Arguments.First() != null &&
                    c.GroupName == JobConstants.NotificationChannels.All));

            await generatedMessages
               .Received(1)
               .AddAsync(
               Arg.Is<SignalRMessage>(
                    c => c.Target == JobConstants.NotificationsTargetFunction &&
                    c.Arguments.Length == 1 &&
                    c.Arguments.First() != null &&
                    c.GroupName == JobConstants.NotificationChannels.ParentJobs));
        }

        [TestMethod]
        public async Task OnNotificationEvent_WhenParentJobWithSpecificationIdIsCreated_ThenSignalRMessagesAdded()
        {
            // Arrange
            NotificationService service = CreateService();

            JobNotification jobNotification = new JobNotification()
            {
                CompletionStatus = null,
                JobId = JobId,
                JobType = "test",
                SpecificationId = SpecificationId,

            };

            string json = JsonConvert.SerializeObject(jobNotification);

            Message message = new Message(Encoding.UTF8.GetBytes(json));

            IAsyncCollector<SignalRMessage> generatedMessages = CreateSignalRMessageCollector();

            // Act
            await service.OnNotificationEvent(message, generatedMessages);

            // Assert
            await generatedMessages
                .Received(3)
                .AddAsync(Arg.Any<SignalRMessage>());

            await generatedMessages
                .Received(1)
                .AddAsync(
                Arg.Is<SignalRMessage>(
                    c => c.Target == JobConstants.NotificationsTargetFunction &&
                    c.Arguments.Length == 1 &&
                    c.Arguments.First() != null &&
                    c.GroupName == JobConstants.NotificationChannels.All));

            await generatedMessages
               .Received(1)
               .AddAsync(
               Arg.Is<SignalRMessage>(
                    c => c.Target == JobConstants.NotificationsTargetFunction &&
                    c.Arguments.Length == 1 &&
                    c.Arguments.First() != null &&
                    c.GroupName == JobConstants.NotificationChannels.ParentJobs));

            await generatedMessages
               .Received(1)
               .AddAsync(
               Arg.Is<SignalRMessage>(
                    c => c.Target == JobConstants.NotificationsTargetFunction &&
                    c.Arguments.Length == 1 &&
                    c.Arguments.First() != null &&
                    c.GroupName == $"{JobConstants.NotificationChannels.SpecificationPrefix}{SpecificationId}"));
        }

        [TestMethod]
        public async Task OnNotificationEvent_WhenChildJobWithSpecificationIdIsCreated_ThenSignalRMessagesAdded()
        {
            // Arrange
            NotificationService service = CreateService();

            JobNotification jobNotification = new JobNotification()
            {
                CompletionStatus = null,
                JobId = JobId,
                JobType = "test",
                SpecificationId = SpecificationId,
                ParentJobId = "parentJobId1",
            };

            string json = JsonConvert.SerializeObject(jobNotification);

            Message message = new Message(Encoding.UTF8.GetBytes(json));

            IAsyncCollector<SignalRMessage> generatedMessages = CreateSignalRMessageCollector();

            // Act
            await service.OnNotificationEvent(message, generatedMessages);

            // Assert
            await generatedMessages
                .Received(2)
                .AddAsync(Arg.Any<SignalRMessage>());

            await generatedMessages
                .Received(1)
                .AddAsync(
                Arg.Is<SignalRMessage>(
                    c => c.Target == JobConstants.NotificationsTargetFunction &&
                    c.Arguments.Length == 1 &&
                    c.Arguments.First() != null &&
                    c.GroupName == JobConstants.NotificationChannels.All));

            await generatedMessages
               .Received(1)
               .AddAsync(
               Arg.Is<SignalRMessage>(
                    c => c.Target == JobConstants.NotificationsTargetFunction &&
                    c.Arguments.Length == 1 &&
                    c.Arguments.First() != null &&
                    c.GroupName == $"{JobConstants.NotificationChannels.SpecificationPrefix}{SpecificationId}"));

            await generatedMessages
               .Received(0)
               .AddAsync(
               Arg.Is<SignalRMessage>(
                    c => c.Target == JobConstants.NotificationsTargetFunction &&
                    c.Arguments.Length == 1 &&
                    c.Arguments.First() != null &&
                    c.GroupName == JobConstants.NotificationChannels.ParentJobs));
        }

        [TestMethod]
        public async Task OnNotificationEvent_WhenJobIsCreated_ThenJobNotificationPropertiesAreSet()
        {
            // Arrange
            NotificationService service = CreateService();

            JobNotification jobNotification = new JobNotification()
            {
                CompletionStatus = CompletionStatus.Succeeded,
                JobId = JobId,
                JobType = "test",
                SpecificationId = SpecificationId,
                ParentJobId = "parentJobId1",
                InvokerUserDisplayName = "invokerDisplayName",
                InvokerUserId = "InvokerUserId",
                ItemCount = 52,
                Outcome = "Outcome text",
                OverallItemsFailed = 2,
                OverallItemsProcessed = 23,
                OverallItemsSucceeded = 21,
                RunningStatus = RunningStatus.InProgress,
                StatusDateTime = new DateTimeOffset(2018, 12, 2, 5, 6, 7, 8, TimeSpan.Zero),
                SupersededByJobId = "jobId",
                Trigger = new Trigger()
                {
                    EntityId = "triggerEntity",
                    EntityType = "triggerEntityType",
                    Message = "message"
                },
            };


            string json = JsonConvert.SerializeObject(jobNotification);

            Message message = new Message(Encoding.UTF8.GetBytes(json));

            IAsyncCollector<SignalRMessage> generatedMessages = CreateSignalRMessageCollector();

            // Act
            await service.OnNotificationEvent(message, generatedMessages);

            // Assert
            await generatedMessages
                .Received(1)
                .AddAsync(
                Arg.Is<SignalRMessage>(
                    c => c.Target == JobConstants.NotificationsTargetFunction &&
                    c.Arguments.Length == 1 &&
                    c.Arguments.First() != null &&
                    c.GroupName == JobConstants.NotificationChannels.All &&
                  ((JobNotification)c.Arguments.First()).CompletionStatus == CompletionStatus.Succeeded &&
                  ((JobNotification)c.Arguments.First()).InvokerUserDisplayName == "invokerDisplayName" &&
                  ((JobNotification)c.Arguments.First()).InvokerUserId == "InvokerUserId" &&
                  ((JobNotification)c.Arguments.First()).ItemCount == 52 &&
                  ((JobNotification)c.Arguments.First()).Outcome == "Outcome text" &&
                  ((JobNotification)c.Arguments.First()).OverallItemsFailed == 2 &&
                  ((JobNotification)c.Arguments.First()).OverallItemsProcessed == 23 &&
                  ((JobNotification)c.Arguments.First()).OverallItemsSucceeded == 21 &&
                  ((JobNotification)c.Arguments.First()).RunningStatus == RunningStatus.InProgress &&
                  ((JobNotification)c.Arguments.First()).StatusDateTime == new DateTimeOffset(2018, 12, 2, 5, 6, 7, 8, TimeSpan.Zero) &&
                  ((JobNotification)c.Arguments.First()).SupersededByJobId == "jobId" &&
                  ((JobNotification)c.Arguments.First()).Trigger.EntityId == "triggerEntity" &&
                  ((JobNotification)c.Arguments.First()).Trigger.EntityType == "triggerEntityType" &&
                  ((JobNotification)c.Arguments.First()).Trigger.Message == "message"
                  ));
        }

        [TestMethod]
        public void OnNotificationEvent_WhenJobNotificationBodyIsNull_ThenErrorThrown()
        {
            // Arrange
            NotificationService service = CreateService();

            JobNotification jobNotification = null;

            string json = JsonConvert.SerializeObject(jobNotification);

            Message message = new Message(Encoding.UTF8.GetBytes(json));

            IAsyncCollector<SignalRMessage> generatedMessages = CreateSignalRMessageCollector();

            // Act
            Func<Task> func = new Func<Task>(async () => { await service.OnNotificationEvent(message, generatedMessages); });

            // Assert
            func
                .Should()
                .Throw<InvalidOperationException>()
                .WithMessage("Job notificiation was null");
        }
    }
}
