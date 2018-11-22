using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Models.Jobs;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Interfaces.ServiceBus;
using CalculateFunding.Services.Jobs;
using CalculateFunding.Services.Jobs.Interfaces;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace CalculateFunding.Services.Calcs
{
    [TestClass]
    public class NotificationServiceTests
    {
        [TestMethod]
        public void SendNotification_WhenNoJobNotification_ThrowsArgumentNullException()
        {
            // Arrange
            INotificationService notificationService = CreateNotificationService();

            Func<Task> action = async () => await notificationService.SendNotification(null);

            // Act and Assert
            action
                .Should()
                .Throw<ArgumentNullException>()
                .And
                .ParamName
                .Should()
                .Be("jobNotification");
        }

        [TestMethod]
        public void SendNotification_WhenNoTrigger_ThrowsArgumentNullException()
        {
            // Arrange
            INotificationService notificationService = CreateNotificationService();

            JobNotification jobNotification = CreateJobNotification();
            jobNotification.Trigger = null;

            Func<Task> action = async () => await notificationService.SendNotification(jobNotification);

            // Act and Assert
            action
                .Should()
                .Throw<ArgumentNullException>()
                .And
                .ParamName
                .Should()
                .Be("Trigger");
        }

        [TestMethod]
        public void SendNotification_WhenNoTriggerEntityId_ThrowsArgumentException()
        {
            // Arrange
            INotificationService notificationService = CreateNotificationService();

            JobNotification jobNotification = CreateJobNotification();
            jobNotification.Trigger.EntityId = string.Empty;

            Func<Task> action = async () => await notificationService.SendNotification(jobNotification);

            // Act and Assert
            action
                .Should()
                .Throw<ArgumentException>()
                .And
                .ParamName
                .Should()
                .Be("EntityId");
        }

        [TestMethod]
        public void SendNotification_WhenNoJobType_ThrowsArgumentException()
        {
            // Arrange
            INotificationService notificationService = CreateNotificationService();

            JobNotification jobNotification = CreateJobNotification();
            jobNotification.JobType = string.Empty;

            Func<Task> action = async () => await notificationService.SendNotification(jobNotification);

            // Act and Assert
            action
                .Should()
                .Throw<ArgumentException>()
                .And
                .ParamName
                .Should()
                .Be("JobType");
        }

        [TestMethod]
        public void SendNotification_WhenNoJobId_ThrowsArgumentException()
        {
            // Arrange
            INotificationService notificationService = CreateNotificationService();

            JobNotification jobNotification = CreateJobNotification();
            jobNotification.JobId = string.Empty;

            Func<Task> action = async () => await notificationService.SendNotification(jobNotification);

            // Act and Assert
            action
                .Should()
                .Throw<ArgumentException>()
                .And
                .ParamName
                .Should()
                .Be("JobId");
        }

        [TestMethod]
        public async Task SendNotification_WhenAllPropertiesSet_AddsMessageToTopic()
        {
            // Arrange
            IDictionary<string, string> topicMessageProperties = null;

            IMessengerService messengerService = CreateMessengerService();
            await messengerService.SendToTopic(Arg.Any<string>(), Arg.Any<JobNotification>(), Arg.Do<IDictionary<string, string>>(p => topicMessageProperties = p));

            INotificationService notificationService = CreateNotificationService(messengerService);

            JobNotification jobNotification = CreateJobNotification();

            // Act
            await notificationService.SendNotification(jobNotification);

            // Assert
            await messengerService
                .Received(1)
                .SendToTopic(Arg.Is(ServiceBusConstants.TopicNames.JobNotifications), Arg.Is(jobNotification), Arg.Any<IDictionary<string, string>>());

            topicMessageProperties.Should().NotBeNull();
            topicMessageProperties["jobId"].Should().Be(jobNotification.JobId, "JobId");
            topicMessageProperties["jobType"].Should().Be(jobNotification.JobType, "JobType");
            topicMessageProperties["entityId"].Should().Be(jobNotification.Trigger.EntityId, "EntityId");
            topicMessageProperties["specificationId"].Should().Be(jobNotification.SpecificationId, "SpecficationId");
        }

        private JobNotification CreateJobNotification()
        {
            return new JobNotification
            {
                InvokerUserDisplayName = "Test User",
                InvokerUserId = "testUser1",
                ItemCount = 12,
                JobId = Guid.NewGuid().ToString(),
                JobType = "Run Calculation",
                RunningStatus = RunningStatus.Queued,
                StatusDateTime = DateTimeOffset.Now,
                Trigger = new Trigger
                {
                    EntityId = Guid.NewGuid().ToString(),
                    EntityType = "Calculation",
                    Message = "Calculation Run requested"
                }
            };
        }

        private INotificationService CreateNotificationService(IMessengerService messengerService = null)
        {
            IJobsResiliencePolicies policies = new ResiliencePolicies
            {
                MessengerServicePolicy = Polly.Policy.NoOpAsync()
            };

            return new NotificationService(messengerService ?? CreateMessengerService(), policies);
        }

        private IMessengerService CreateMessengerService()
        {
            return Substitute.For<IMessengerService>();
        }
    }
}
