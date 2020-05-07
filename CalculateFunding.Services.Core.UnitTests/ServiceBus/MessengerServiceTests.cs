using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.ServiceBus;
using CalculateFunding.Common.ServiceBus.Interfaces;
using CalculateFunding.Common.ServiceBus.Options;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.UnitTests;
using FluentAssertions;
using Microsoft.Azure.ServiceBus;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureCore = Microsoft.Azure.ServiceBus.Core;
using AzureServiceBus = Microsoft.Azure.ServiceBus;

namespace CalculateFunding.Services.Core.ServiceBus
{
    [TestClass]
    public class MessengerServiceTests
    {
        private MessengerService _messengerService;
        private Mock<IMessageReceiverFactory> _messageReceiverFactory;
        private Mock<AzureCore.IMessageReceiver> _azureMessageReceiver;
        private Mock<IManagementClient> _managementClient;
        private Mock<ITopicClient> _topicClient;
        private Mock<AzureServiceBus.IQueueClient> _queueClient;
        private const string TopicName = "TopicName";
        private const string SubscriptionName = "SubscriptionName";
        private const string QueueName = "QueueName";

        [TestInitialize]
        public void SetUp()
        {
            _azureMessageReceiver = new Mock<AzureCore.IMessageReceiver>();
            _managementClient = new Mock<IManagementClient>();
            _messageReceiverFactory = new Mock<IMessageReceiverFactory>();
            _topicClient = new Mock<ITopicClient>();
            _queueClient = new Mock<AzureServiceBus.IQueueClient>();
            _messengerService = new MessengerService(new ServiceBusSettings { ConnectionString = "ConnectionString" },
                _managementClient.Object,
                _messageReceiverFactory.Object);
        }

        [TestMethod]
        public async Task IsHealthy_PeekSuccessful_ReturnsTrue()
        {
            GivenMessageReceiverReturned(QueueName);
            GivenPeekSuccessful();
            bool isHealthy = await WhenHealthChecked();
            ThenServiceIsHealthy(isHealthy);
            _azureMessageReceiver.VerifyAll();
            _messageReceiverFactory.VerifyAll();
        }

        [TestMethod]
        public async Task IsHealthy_PeekUnsuccessful_ReturnsTrue()
        {
            GivenMessageReceiverReturned(QueueName);
            bool isHealthy = await WhenHealthChecked();
            ThenServiceIsUnhealthy(isHealthy);
            _messageReceiverFactory.VerifyAll();
        }

        [TestMethod]
        public async Task ReceiveMessages_TargetMessageReturnedFromServiceBus_TargetMessageReceived()
        {
            Guid lockToken = Guid.NewGuid();
            Job job = new Job { JobDefinitionId = JobConstants.DefinitionNames.PopulateScopedProvidersJob };
            Predicate<Job> predicate = _ => _.JobDefinitionId == JobConstants.DefinitionNames.PopulateScopedProvidersJob;

            _messageReceiverFactory.Setup(_ => _.Receiver(QueueName))
                .Returns(_azureMessageReceiver.Object);

            Message message = NewMessage(_ => _.WithLockToken(lockToken)
                .WithBody(JsonConvert.SerializeObject(job).AsUTF8Bytes()));

            GivenMessageReceiverReturned(QueueName);
            AndMessageReturnedFromServiceBus(message);
            Job jobReceived = await WhenMessagesReceived(QueueName, predicate);
            ThenTargetMessageReceived(job, predicate);
            _azureMessageReceiver.VerifyAll();
            _messageReceiverFactory.VerifyAll();
        }

        [TestMethod]
        public async Task ReceiveMessages_TargetMessageNotReturnedFromServiceBus_TimesOut()
        {
            Guid lockToken = Guid.NewGuid();
            Job job = new Job { JobDefinitionId = JobConstants.DefinitionNames.PopulateScopedProvidersJob };
            Predicate<Job> predicate = _ => _.JobDefinitionId == JobConstants.DefinitionNames.PopulateScopedProvidersJob;

            GivenMessageReceiverReturned(QueueName);
            await WhenMessagesReceived(QueueName, predicate);
            ThenReceiverTimesOut();
            _messageReceiverFactory.VerifyAll();
        }

        [TestMethod]
        public async Task CreateSubscription_SubscriptionGetsCreated()
        {
            await _messengerService.CreateSubscription(TopicName, SubscriptionName);
            _managementClient.Verify(_ => _.CreateSubscription(
                TopicName,
                SubscriptionName)
            );
        }

        [TestMethod]
        public async Task DeleteSubscription_SubscriptionGetsDeleted()
        {
            await _messengerService.DeleteSubscription(TopicName, SubscriptionName);
            _managementClient.Verify(_ => _.DeleteSubscription(
                TopicName,
                SubscriptionName)
            );
        }

        [TestMethod]
        public async Task CreateQueue_QueueGetsCreated()
        {
            await _messengerService.CreateQueue(QueueName);
            _managementClient.Verify(_ => _.CreateQueue(
                QueueName)
            );
        }

        [TestMethod]
        public async Task DeleteQueue_QueueGetsDeleted()
        {
            await _messengerService.DeleteQueue(QueueName);
            _managementClient.Verify(_ => _.DeleteQueue(
                QueueName)
            );
        }

        [TestMethod]
        public async Task CreateTopic_TopicGetsCreated()
        {
            await _messengerService.CreateTopic(TopicName);
            _managementClient.Verify(_ => _.CreateTopic(
                TopicName)
            );
        }

        [TestMethod]
        public async Task DeleteTopic_TopicGetsDeleted()
        {
            await _messengerService.DeleteTopic(TopicName);
            _managementClient.Verify(_ => _.DeleteTopic(
                TopicName)
            );
        }

        [TestMethod]
        public async Task SendToTopic_MessageSentToTopic()
        {
            string sessionId = Guid.NewGuid().ToString();
            Job job = new Job { JobDefinitionId = JobConstants.DefinitionNames.PopulateScopedProvidersJob };
            Message message = NewMessage(_ => _.WithUserProperty("job_id", "job_id"));

            GivenTopic(TopicName);

            await WhenMessageSentToTopic(TopicName,
                job,
                message.UserProperties.ToDictionary(_ => _.Key, _ => _.Value.ToString()),
                sessionId: sessionId);

            ThenMessageSentToTopic(sessionId);
            _managementClient.VerifyAll();
        }

        [TestMethod]
        public async Task SendToQueue_MessageSentToQueue()
        {
            string sessionId = Guid.NewGuid().ToString();
            Job job = new Job { JobDefinitionId = JobConstants.DefinitionNames.PopulateScopedProvidersJob };
            Message message = NewMessage(_ => _.WithUserProperty("job_id", "job_id"));

            GivenQueue(QueueName);

            await WhenMessageSentToQueue(QueueName,
                job,
                message.UserProperties.ToDictionary(_ => _.Key, _ => _.Value.ToString()),
                sessionId: sessionId);

            ThenMessageSentToQueue(sessionId);
            _managementClient.VerifyAll();
        }

        private void GivenPeekSuccessful()
        {
            _azureMessageReceiver.Setup(_ => _.PeekAsync(1))
                .ReturnsAsync(new List<Message>())
                .Verifiable();
        }

        private void GivenTopic(string topicName)
        {
            _managementClient.Setup(_ => _.GetTopicClient(It.Is<string>(_ => _ == topicName)))
                .Returns(_topicClient.Object)
                .Verifiable(); ;
        }

        private void GivenQueue(string queueName)
        {
            _managementClient.Setup(_ => _.GetQueueClient(It.Is<string>(_ => _ == queueName)))
                .Returns(_queueClient.Object)
                .Verifiable();
        }

        private void GivenMessageReceiverReturned(string queueName)
        {
            _messageReceiverFactory.Setup(_ => _.Receiver(queueName))
                .Returns(_azureMessageReceiver.Object)
                .Verifiable();
        }

        private void AndMessageReturnedFromServiceBus(Message message)
        {
            _azureMessageReceiver.Setup(_ => _.ReceiveAsync(It.Is<TimeSpan>(_ => _.TotalSeconds == 5)))
                .ReturnsAsync(message)
                .Verifiable();
        }

        private async Task<bool> WhenHealthChecked()
        {
            (bool isHealthy, string message) = await _messengerService.IsHealthOk(QueueName);

            return isHealthy;
        }

        private async Task<T> WhenMessagesReceived<T>(string entityPath, Predicate<T> predicate)
            where T:class
        {
            return await _messengerService.ReceiveMessage(entityPath, predicate, TimeSpan.FromSeconds(5));
        }

        private async Task WhenMessageSentToTopic<T>(string topicName, T data, IDictionary<string, string> properties, string sessionId)
            where T:class
        {
            await _messengerService.SendToTopic<T>(topicName,
                data,
                properties,
                sessionId: sessionId);
        }

        private async Task WhenMessageSentToQueue<T>(string queueName, T data, IDictionary<string, string> properties, string sessionId)
            where T : class
        {
            await _messengerService.SendToQueue(queueName,
                data,
                properties,
                sessionId: sessionId);
        }

        private void ThenMessageSentToTopic(string sessionId)
        {
            _topicClient.Verify(_ => _.SendAsync(It.Is<Message>(_ => _.SessionId == sessionId)));
        }

        private void ThenMessageSentToQueue(string sessionId)
        {
            _queueClient.Verify(_ => _.SendAsync(It.Is<Message>(_ => _.SessionId == sessionId)));
        }

        private void ThenTargetMessageReceived<T>(T message, Predicate<T> predicate)
        {
            predicate(message)
                .Should()
                .BeTrue();
        }

        private void ThenReceiverTimesOut()
        {
            _messageReceiverFactory.Verify(_ => _.TimedOut());
        }

        private void ThenServiceIsHealthy(bool isHealthy)
        {
            isHealthy
                .Should()
                .BeTrue();
        }

        private void ThenServiceIsUnhealthy(bool isHealthy)
        {
            isHealthy
                .Should()
                .BeTrue();
        }

        private Message NewMessage(Action<MessageBuilder> setup = null)
        {
            MessageBuilder messageBuilder = new MessageBuilder();

            setup?.Invoke(messageBuilder);

            return messageBuilder.Build();
        }
    }
}
