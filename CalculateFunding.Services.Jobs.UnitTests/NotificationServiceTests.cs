using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.ServiceBus.Interfaces;
using CalculateFunding.Models.Jobs;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Jobs.Interfaces;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Serilog;

namespace CalculateFunding.Services.Jobs.Services
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

            JobSummary jobSummary = CreateJobSummary();
            jobSummary.Trigger = null;

            Func<Task> action = async () => await notificationService.SendNotification(jobSummary);

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
        public void SendNotification_WhenNoJobType_ThrowsArgumentException()
        {
            // Arrange
            INotificationService notificationService = CreateNotificationService();

            JobSummary jobSummary = CreateJobSummary();
            jobSummary.JobType = string.Empty;

            Func<Task> action = async () => await notificationService.SendNotification(jobSummary);

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

            JobSummary jobSummary = CreateJobSummary();
            jobSummary.JobId = string.Empty;

            Func<Task> action = async () => await notificationService.SendNotification(jobSummary);

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
            await messengerService.SendToTopic(Arg.Any<string>(), Arg.Any<JobSummary>(), Arg.Do<IDictionary<string, string>>(p => topicMessageProperties = p));

            ILogger logger = CreateLogger();

            INotificationService notificationService = CreateNotificationService(messengerService, logger);

            JobSummary jobSummary = CreateJobSummary();

            // Act
            await notificationService.SendNotification(jobSummary);

            // Assert
            await messengerService
                .Received(1)
                .SendToTopic(Arg.Is(ServiceBusConstants.TopicNames.JobNotifications), Arg.Is(jobSummary), Arg.Any<IDictionary<string, string>>());

            topicMessageProperties.Should().NotBeNull();
            topicMessageProperties["jobId"].Should().Be(jobSummary.JobId, "JobId");
            topicMessageProperties["jobType"].Should().Be(jobSummary.JobType, "JobType");
            topicMessageProperties["entityId"].Should().Be(jobSummary.Trigger.EntityId, "EntityId");
            topicMessageProperties["specificationId"].Should().Be(jobSummary.SpecificationId, "SpecficationId");
            topicMessageProperties["parentJobId"].Should().Be(jobSummary.ParentJobId, "ParentJobId");

            logger
                .Received(1)
                .Information(
                Arg.Is("Sent notification for job with id '{JobId}' of type '{JobType}' for entity '{EntityType}' with id '{EntityId} and status '{CompletionStatus}"), 
                Arg.Is(jobSummary.JobId), 
                Arg.Is(jobSummary.JobType), 
                Arg.Is(jobSummary.Trigger.EntityType), 
                Arg.Is(jobSummary.Trigger.EntityId), 
                Arg.Is(jobSummary.CompletionStatus));
        }

        private JobSummary CreateJobSummary()
        {
            return new JobSummary
            {
                InvokerUserDisplayName = "Test User",
                InvokerUserId = "testUser1",
                ItemCount = 12,
                JobId = Guid.NewGuid().ToString(),
                JobType = "Run Calculation",
                RunningStatus = RunningStatus.Queued,
                StatusDateTime = DateTimeOffset.Now,
                ParentJobId = "parent-job-1",
                Trigger = new Trigger
                {
                    EntityId = Guid.NewGuid().ToString(),
                    EntityType = "Calculation",
                    Message = "Calculation Run requested"
                },
                Created = new DateTimeOffset(new DateTime(2020, 1, 1))
            };
        }

        private INotificationService CreateNotificationService(IMessengerService messengerService = null, ILogger logger = null)
        {
            IJobsResiliencePolicies policies = JobsResilienceTestHelper.GenerateTestPolicies();

            return new NotificationService(messengerService ?? CreateMessengerService(), policies, logger ?? CreateLogger());
        }

        private IMessengerService CreateMessengerService()
        {
            return Substitute.For<IMessengerService>();
        }

        private ILogger CreateLogger()
        {
            return Substitute.For<ILogger>();
        }
    }
}
