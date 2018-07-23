using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Interfaces.ServiceBus;
using CalculateFunding.Services.Core.Options;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;
using Newtonsoft.Json;


namespace CalculateFunding.Services.Core.ServiceBus
{
    public class MessengerService : IMessengerService
    {
        private static readonly ConcurrentDictionary<string, QueueClient> _queueClients = new ConcurrentDictionary<string, QueueClient>();
        private static readonly ConcurrentDictionary<string, TopicClient> _topicClients = new ConcurrentDictionary<string, TopicClient>();
        private readonly string _connectionString;

        public MessengerService(ServiceBusSettings settings)
        {
            _connectionString = settings.ConnectionString;
        }

        public async Task<(bool Ok, string Message)> IsHealthOk(string queueName)
        {
            try
            {
                // Only way to check if connection string is correct is try receiving a message, which isn't possible for topics as don't have a subscription
                var receiver = new MessageReceiver(_connectionString, queueName);
                var message = await receiver.PeekAsync(1);
                await receiver.CloseAsync();
                return await Task.FromResult((true, string.Empty));
            }
            catch (ServiceBusCommunicationException ex)
            {
                return (false, ex.Message);
            }
        }

        QueueClient GetQueueClient(string queueName)
        {
            return _queueClients.GetOrAdd(queueName, (key) => {
                return new QueueClient(_connectionString, key, ReceiveMode.PeekLock, RetryExponential.Default);
            });
        }

        public async Task SendToQueue<T>(string queueName, T data, IDictionary<string, string> properties) where T : class
        {
            var queueClient = GetQueueClient(queueName);

            var json = JsonConvert.SerializeObject(data);

            Message message = new Message(Encoding.UTF8.GetBytes(json));
            message.PartitionKey = Guid.NewGuid().ToString();

            foreach (var property in properties)
                message.UserProperties.Add(property.Key, property.Value);

            await queueClient.SendAsync(message);
        }

        TopicClient GetTopicClient(string topicName)
        {
            return _topicClients.GetOrAdd(topicName, (key) => {
                return new TopicClient(_connectionString, key, RetryPolicy.Default);
            });
        }

        public async Task SendToTopic<T>(string topicName, T data, IDictionary<string, string> properties) where T : class
        {
            var topicClient = GetTopicClient(topicName);

            var json = JsonConvert.SerializeObject(data);

            Message message = new Message(Encoding.UTF8.GetBytes(json));
            message.PartitionKey = Guid.NewGuid().ToString();

            foreach (var property in properties)
                message.UserProperties.Add(property.Key, property.Value);

            await topicClient.SendAsync(message);
        }
    }
}
