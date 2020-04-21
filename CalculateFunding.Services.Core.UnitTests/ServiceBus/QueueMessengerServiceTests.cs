using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Interfaces.ServiceBus;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.WindowsAzure.Storage.Queue;
using Moq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Core.ServiceBus
{
    [TestClass]
    public class QueueMessengerServiceTests
    {
        private QueueMessengerService _messengerService;
        private Mock<IQueueClient> _queueClient;
        private const string QueueName = "QueueName";
        private const string TopicName = "TopicName";

        [TestInitialize]
        public void SetUp()
        {
            _queueClient = new Mock<IQueueClient>();
            _messengerService = new QueueMessengerService(_queueClient.Object);
        }

        [TestMethod]
        public async Task IsHealthy_ReturnsQueueExists_ReturnsTrue()
        {
            GivenQueueExists();
            bool isHealthy = await WhenHealthChecked();
            ThenServiceIsHealthy(isHealthy);
            _queueClient.VerifyAll();
        }

        [TestMethod]
        public async Task IsHealthy_ReturnsQueueDoesNotExists_ReturnsFalse()
        {
            bool isHealthy = await WhenHealthChecked();
            ThenServiceIsUnHealthy(isHealthy);
        }

        [TestMethod]
        public async Task ReceiveMessages_TargetMessageReturnedFromStorageQueue_TargetMessageReceived()
        {
            Guid lockToken = Guid.NewGuid();
            Job job = new Job { JobDefinitionId = JobConstants.DefinitionNames.PopulateScopedProvidersJob };
            Predicate<Job> predicate = _ => _.JobDefinitionId == JobConstants.DefinitionNames.PopulateScopedProvidersJob;

            CloudQueueMessage message = new CloudQueueMessage(JsonConvert.SerializeObject(new QueueMessage<Job> { Data = job }));

            GivenMessageReturnedFromQueue(message);
            Job jobReceived = await WhenMessagesReceived(QueueName, predicate);
            ThenTargetMessageReceived(job, predicate);
            _queueClient.VerifyAll();
        }

        [TestMethod]
        public async Task ReceiveMessages_TargetMessageNotReturnedFromServiceBus_TimesOut()
        {
            Guid lockToken = Guid.NewGuid();
            Job job = new Job { JobDefinitionId = JobConstants.DefinitionNames.PopulateScopedProvidersJob };
            Predicate<Job> predicate = _ => _.JobDefinitionId == JobConstants.DefinitionNames.PopulateScopedProvidersJob;

            await WhenMessagesReceived(QueueName, predicate);
            ThenReceiverTimesOut();
        }

        [TestMethod]
        public async Task CreateQueue_QueueGetsCreated()
        {
            await _messengerService.CreateQueue(QueueName);
            _queueClient.Verify(_ => _.CreateQueue(
                QueueName)
            );
        }

        [TestMethod]
        public async Task DeleteQueue_QueueGetsDeleted()
        {
            await _messengerService.DeleteQueue(QueueName);
            _queueClient.Verify(_ => _.DeleteQueue(
                QueueName)
            );
        }

        [TestMethod]
        public async Task SendToQueue_MessageSentToQueue()
        {
            string sessionId = Guid.NewGuid().ToString();
            Job job = new Job { JobDefinitionId = JobConstants.DefinitionNames.PopulateScopedProvidersJob };
            QueueMessage<Job> queueMessage = new QueueMessage<Job>
            {
                Data = job,
                UserProperties = null
            };
            CloudQueueMessage message = new CloudQueueMessage(JsonConvert.SerializeObject(queueMessage));

            await WhenMessageSentToQueue(QueueName,
                job,
                null,
                sessionId: sessionId);

            ThenMessageSentToQueue(QueueName, message);
        }

        [TestMethod]
        public async Task SendToQueue_WithCompression_MessageSentToQueue()
        {
            string sessionId = Guid.NewGuid().ToString();
            Job job = new Job { JobDefinitionId = JobConstants.DefinitionNames.PopulateScopedProvidersJob };

            await WhenMessageSentToQueue(QueueName,
                job,
                null,
                sessionId: sessionId,
                compression: true);

            ThenMessageSentToQueueWithCompression(QueueName);
        }

        [TestMethod]
        public async Task SendToQueueAsJson_MessageSentToQueue()
        {
            string sessionId = Guid.NewGuid().ToString();
            Job job = new Job { JobDefinitionId = JobConstants.DefinitionNames.PopulateScopedProvidersJob };
            string jobAsString = JsonConvert.SerializeObject(job);
            CloudQueueMessage message = new CloudQueueMessage($"{{\"Data\":{jobAsString},\"UserProperties\":null}}");

            await WhenMessageSentToQueueAsJson(QueueName,
                jobAsString,
                null,
                sessionId: sessionId);

            ThenMessageSentToQueue(QueueName, message);
        }


        [TestMethod]
        public async Task SendToQueueAsJson_WithCompression_MessageSentToQueue()
        {
            string sessionId = Guid.NewGuid().ToString();
            Job job = new Job { JobDefinitionId = JobConstants.DefinitionNames.PopulateScopedProvidersJob };
            string jobAsString = JsonConvert.SerializeObject(job);

            await WhenMessageSentToQueueAsJson(QueueName,
                jobAsString,
                null,
                sessionId: sessionId,
                compression:true);

            ThenMessageSentToQueueWithCompression(QueueName);
        }

        [TestMethod]
        public async Task SendToTopic_MessageSentToQueue()
        {
            string sessionId = Guid.NewGuid().ToString();
            Job job = new Job { JobDefinitionId = JobConstants.DefinitionNames.PopulateScopedProvidersJob };
            QueueMessage<Job> queueMessage = new QueueMessage<Job>
            {
                Data = job,
                UserProperties = null
            };
            CloudQueueMessage message = new CloudQueueMessage(JsonConvert.SerializeObject(queueMessage));

            await WhenMessageSentToTopic(TopicName,
                job,
                null,
                sessionId: sessionId);

            ThenMessageSentToQueue(TopicName, message);
        }

        [TestMethod]
        public async Task SendToTopicAsJson_MessageSentToQueue()
        {
            string sessionId = Guid.NewGuid().ToString();
            Job job = new Job { JobDefinitionId = JobConstants.DefinitionNames.PopulateScopedProvidersJob };
            string jobAsString = JsonConvert.SerializeObject(job);
            CloudQueueMessage message = new CloudQueueMessage($"{{\"Data\":{jobAsString},\"UserProperties\":null}}");

            await WhenMessageSentToTopicAsJson(QueueName,
                jobAsString,
                null,
                sessionId: sessionId);

            ThenMessageSentToQueue(QueueName, message);
        }

        [TestMethod]
        public async Task SendToTopicAsJson_WithCompression_MessageSentToQueue()
        {
            string sessionId = Guid.NewGuid().ToString();
            Job job = new Job { JobDefinitionId = JobConstants.DefinitionNames.PopulateScopedProvidersJob };
            string jobAsString = JsonConvert.SerializeObject(job);

            await WhenMessageSentToTopicAsJson(QueueName,
                jobAsString,
                null,
                sessionId: sessionId,
                compression:true);

            ThenMessageSentToQueueWithCompression(QueueName);
        }

        private void GivenQueueExists()
        {
            _queueClient.Setup(_ => _.Exists(QueueName))
                .ReturnsAsync(true)
                .Verifiable();
        }

        private void GivenMessageReturnedFromQueue(CloudQueueMessage message)
        {
            _queueClient.Setup(_ => _.GetMessage(QueueName))
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
            await _messengerService.SendToTopic(topicName,
                data,
                properties,
                sessionId: sessionId);
        }

        private async Task WhenMessageSentToTopicAsJson(string topicName, string data, IDictionary<string, string> properties, string sessionId, bool compression = false)
        {
            await _messengerService.SendToTopicAsJson(topicName,
                data,
                properties,
                sessionId: sessionId,
                compressData: compression);
        }

        private async Task WhenMessageSentToQueue<T>(string queueName, T data, IDictionary<string, string> properties, string sessionId, bool compression = false)
            where T:class
        {
            await _messengerService.SendToQueue(queueName,
                data,
                properties,
                sessionId: sessionId,
                compressData: compression);
        }

        private async Task WhenMessageSentToQueueAsJson(string queueName, string data, IDictionary<string, string> properties, string sessionId, bool compression = false)
        {
            await _messengerService.SendToQueueAsJson(queueName,
                data,
                properties,
                sessionId: sessionId,
                compressData: compression);
        }

        private void ThenTargetMessageReceived<T>(T message, Predicate<T> predicate)
        {
            predicate(message)
                .Should()
                .BeTrue();
        }

        private void ThenReceiverTimesOut()
        {
            _queueClient.Verify(_ => _.TimedOut());
        }

        private void ThenMessageSentToQueue(string queueName, CloudQueueMessage message)
        {
            _queueClient.Verify(_ => _.AddMessage(queueName, It.Is<CloudQueueMessage>(_ => _.AsString == message.AsString)));
        }

        private void ThenMessageSentToQueueWithCompression(string queueName)
        {
            _queueClient.Verify(_ => _.AddMessage(queueName, It.IsAny<CloudQueueMessage>()));
        }

        private void ThenServiceIsHealthy(bool isHealthy)
        {
            isHealthy
                .Should()
                .BeTrue();
        }

        private void ThenServiceIsUnHealthy(bool isHealthy)
        {
            isHealthy
                .Should()
                .BeFalse();
        }
    }
}
