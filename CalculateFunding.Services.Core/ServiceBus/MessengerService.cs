using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core.Interfaces.ServiceBus;
using CalculateFunding.Services.Core.Options;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;
using Microsoft.Azure.ServiceBus.Management;
using Newtonsoft.Json;

namespace CalculateFunding.Services.Core.ServiceBus
{
    public class MessengerService : BaseMessengerService, IMessengerService, IServiceBusService
    {
        private static readonly ConcurrentDictionary<string, QueueClient> _queueClients = new ConcurrentDictionary<string, QueueClient>();
        private static readonly ConcurrentDictionary<string, TopicClient> _topicClients = new ConcurrentDictionary<string, TopicClient>();
        private static readonly ConcurrentDictionary<string, SubscriptionClient> _subscriptionClients = new ConcurrentDictionary<string, SubscriptionClient>();
        private readonly string _connectionString;

        public string ServiceName { get; }

        public MessengerService(ServiceBusSettings settings, string serviceName = null)
        {
            _connectionString = settings.ConnectionString;
            ServiceName = serviceName;
        }

        public async Task<(bool Ok, string Message)> IsHealthOk(string queueName)
        {
            try
            {
                // Only way to check if connection string is correct is try receiving a message, 
                // which isn't possible for topics as don't have a subscription
                MessageReceiver receiver = new MessageReceiver(_connectionString, queueName);
                IList<Message> message = await receiver.PeekAsync(1);
                await receiver.CloseAsync();
                return await Task.FromResult((true, string.Empty));
            }
            catch (ServiceBusCommunicationException ex)
            {
                return (false, ex.Message);
            }
        }

        public async Task CreateSubscription(string topicName, string subscriptionName)
        {
            ManagementClient managementClient = new ManagementClient(_connectionString);
            await managementClient.CreateSubscriptionAsync(topicName, subscriptionName);
        }

        public async Task DeleteSubscription(string topicName, string subscriptionName)
        {
            ManagementClient managementClient = new ManagementClient(_connectionString);
            await managementClient.DeleteSubscriptionAsync(topicName, subscriptionName);
        }

        public async Task CreateQueue(string queuePath)
        {
            ManagementClient managementClient = new ManagementClient(_connectionString);
            await managementClient.CreateQueueAsync(queuePath);
        }

        public async Task DeleteQueue(string queuePath)
        {
            ManagementClient managementClient = new ManagementClient(_connectionString);
            await managementClient.DeleteQueueAsync(queuePath);
        }

        public async Task CreateTopic(string topicName)
        {
            ManagementClient managementClient = new ManagementClient(_connectionString);
            await managementClient.CreateTopicAsync(topicName);
        }

        public async Task DeleteTopic(string topicName)
        {
            ManagementClient managementClient = new ManagementClient(_connectionString);
            await managementClient.DeleteTopicAsync(topicName);
        }

        protected async override Task ReceiveMessages<T>(string entityPath, Predicate<T> predicate, TimeSpan timeout)
        {
            List<T> messages = new List<T>();
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            MessageReceiver receiver = new MessageReceiver(_connectionString, entityPath);

            _ = Task.Run(() =>
            {
                if (!cancellationTokenSource.Token.WaitHandle.WaitOne(timeout))
                {
                    cancellationTokenSource.Cancel();
                }
            });

            try
            {
                while (true)
                {
                    Message message = await receiver.ReceiveAsync(TimeSpan.FromSeconds(5));

                    if (message != null)
                    {
                        await receiver.CompleteAsync(message.SystemProperties.LockToken);
                        string json = null;

                        using (MemoryStream inputStream = new MemoryStream(message.Body))
                        {
                            using (StreamReader streamReader = new StreamReader(inputStream))
                            {
                                json = streamReader.ReadToEnd();
                            }
                        }

                        T messageOfType = JsonConvert.DeserializeObject<T>(json);

                        if (predicate(messageOfType))
                        {
                            break;
                        }
                    }

                    if (cancellationTokenSource.Token.IsCancellationRequested)
                        break;
                }
            }
            finally
            {
                // cancel timeout
                cancellationTokenSource.Cancel();
                await receiver.CloseAsync();
            }
        }

        private QueueClient GetQueueClient(string queueName)
        {
            return _queueClients.GetOrAdd(queueName, (key) =>
            {
                return new QueueClient(_connectionString, key, ReceiveMode.PeekLock, RetryExponential.Default);
            });
        }

        public async Task SendToQueue<T>(string queueName, 
            T data, 
            IDictionary<string, string> properties, 
            bool compressData = false, 
            string sessionId = null) where T : class
        {
            string json = JsonConvert.SerializeObject(data);

            await SendToQueueAsJson(queueName, json, properties, compressData, sessionId);
        }

        public async Task SendToQueueAsJson(string queueName, 
            string data, 
            IDictionary<string, string> properties, 
            bool compressData = false, 
            string sessionId = null)
        {
            Guard.IsNullOrWhiteSpace(queueName, nameof(queueName));

            Message message = ConstructMessage(data, compressData, sessionId);

            foreach (KeyValuePair<string, string> property in properties)
            {
                message.UserProperties.Add(property.Key, property.Value);
            }

            QueueClient queueClient = GetQueueClient(queueName);

            await queueClient.SendAsync(message);
        }

        private TopicClient GetTopicClient(string topicName)
        {
            return _topicClients.GetOrAdd(topicName, (key) =>
            {
                return new TopicClient(_connectionString, key, RetryPolicy.Default);
            });
        }

        public async Task SendToTopic<T>(string topicName,
            T data, 
            IDictionary<string, string> properties, 
            bool compressData = false, 
            string sessionId = null) where T : class
        {
            string json = JsonConvert.SerializeObject(data);

            await SendToTopicAsJson(topicName, json, properties, compressData, sessionId);
        }

        public async Task SendToTopicAsJson(string topicName, 
            string data, 
            IDictionary<string, string> properties, 
            bool compressData = false, 
            string sessionId = null)
        {
            Guard.IsNullOrWhiteSpace(topicName, nameof(topicName));

            Message message = ConstructMessage(data, compressData, sessionId);

            foreach (KeyValuePair<string, string> property in properties)
            {
                message.UserProperties.Add(property.Key, property.Value);
            }

            TopicClient topicClient = GetTopicClient(topicName);

            await topicClient.SendAsync(message);
        }

        private static Message ConstructMessage(string data, bool compressData, string sessionId = null)
        {
            Message message = null;

            if (!string.IsNullOrWhiteSpace(data))
            {
                byte[] bytes = compressData ? data.Compress() : Encoding.UTF8.GetBytes(data);

                message = new Message(bytes)
                {
                    PartitionKey = Guid.NewGuid().ToString()
                };

                if (compressData)
                {
                    message.UserProperties.Add("compressed", true);
                }

            }
            else
            {
                message = new Message()
                {
                    PartitionKey = Guid.NewGuid().ToString()
                };
            }

            if (!string.IsNullOrWhiteSpace(sessionId))
            {
                message.SessionId = sessionId;
            }

            return message;
        }
    }
}
