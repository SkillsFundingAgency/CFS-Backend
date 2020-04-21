using CalculateFunding.Services.Core.Interfaces.ServiceBus;
using Microsoft.Azure.ServiceBus;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using AzureManagement = Microsoft.Azure.ServiceBus.Management;
using AzureServiceBus = Microsoft.Azure.ServiceBus;

namespace CalculateFunding.Services.Core.ServiceBus
{
    public class ManagementClient : IManagementClient
    {
        private static readonly ConcurrentDictionary<string, AzureServiceBus.IQueueClient> _queueClients = new ConcurrentDictionary<string, AzureServiceBus.IQueueClient>();
        private static readonly ConcurrentDictionary<string, ITopicClient> _topicClients = new ConcurrentDictionary<string, ITopicClient>();
        private readonly string _connectionString;

        private AzureManagement.ManagementClient Client => new AzureManagement.ManagementClient(_connectionString);

        public ManagementClient(string connectionString)
        {
            _connectionString = connectionString;
        }

        public ITopicClient GetTopicClient(string topicName)
        {
            return _topicClients.GetOrAdd(topicName, (key) =>
            {
                return new TopicClient(_connectionString, key, RetryPolicy.Default);
            });
        }

        public AzureServiceBus.IQueueClient GetQueueClient(string queueName)
        {
            return _queueClients.GetOrAdd(queueName, (key) =>
            {
                return new AzureServiceBus.QueueClient(_connectionString, key, ReceiveMode.PeekLock, RetryExponential.Default);
            });
        }

        public async Task CreateSubscription(string topicName, string subscriptionName)
        {
            await Client.CreateSubscriptionAsync(topicName, subscriptionName);
        }

        public async Task DeleteSubscription(string topicName, string subscriptionName)
        {
            await Client.DeleteSubscriptionAsync(topicName, subscriptionName);
        }

        public async Task<AzureManagement.QueueDescription> CreateQueue(string queuePath)
        {
            return await Client.CreateQueueAsync(queuePath);
        }

        public async Task DeleteQueue(string queuePath)
        {
            await Client.DeleteQueueAsync(queuePath);
        }

        public async Task<AzureManagement.TopicDescription> CreateTopic(string topicName)
        {
            return await Client.CreateTopicAsync(topicName);
        }

        public async Task DeleteTopic(string topicName)
        {
            await Client.DeleteTopicAsync(topicName);
        }
    }
}
