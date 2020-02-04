using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
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
        private readonly string _serviceName;

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
                // Only way to check if connection string is correct is try receiving a message, which isn't possible for topics as don't have a subscription
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

        public async Task<IEnumerable<T>> ReceiveMessages<T>(string entityPath, TimeSpan timeout) where T : class
        {
            List<T> messages = new List<T>();
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            MessageReceiver receiver = new MessageReceiver(_connectionString, entityPath);

            _ = Task.Run(() =>
            {
                Thread.Sleep(timeout);
                cancellationTokenSource.Cancel();
            });

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

                    messages.Add(JsonConvert.DeserializeObject<T>(json));
                }

                if (cancellationTokenSource.Token.IsCancellationRequested)
                    break;
            }

            await receiver.CloseAsync();

            return messages;
        }

        private QueueClient GetQueueClient(string queueName)
        {
            return _queueClients.GetOrAdd(queueName, (key) =>
            {
                return new QueueClient(_connectionString, key, ReceiveMode.PeekLock, RetryExponential.Default);
            });
        }

        public async Task SendToQueue<T>(string queueName, T data, IDictionary<string, string> properties, bool compressData = false, string sessionId = null) where T : class
        {
            string json = JsonConvert.SerializeObject(data);

            await SendToQueueAsJson(queueName, json, properties, compressData, sessionId);
        }

        public async Task SendToQueueAsJson(string queueName, string data, IDictionary<string, string> properties, bool compressData = false, string sessionId = null)
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

        public async Task SendToTopic<T>(string topicName, T data, IDictionary<string, string> properties, bool compressData = false, string sessionId = null) where T : class
        {
            string json = JsonConvert.SerializeObject(data);

            await SendToTopicAsJson(topicName, json, properties, compressData, sessionId);
        }

        public async Task SendToTopicAsJson(string topicName, string data, IDictionary<string, string> properties, bool compressData = false, string sessionId = null)
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
