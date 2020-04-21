using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core.Interfaces.ServiceBus;
using Microsoft.WindowsAzure.Storage.Queue;
using Newtonsoft.Json;
using System.Threading;

namespace CalculateFunding.Services.Core.ServiceBus
{
    public class QueueMessengerService : BaseMessengerService, IMessengerService, IQueueService
    {
        private IQueueClient _client;

        public string ServiceName { get; }

        public QueueMessengerService(IQueueClient client, string serviceName = null)
        {
            _client = client;
            ServiceName = serviceName;
        }

        public async Task<(bool Ok, string Message)> IsHealthOk(string queueName)
        {
            try
            {
                bool result = await _client.Exists(queueName);
                return (result, string.Empty);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        public async Task CreateQueue(string entityPath)
        {
            await _client.CreateQueue(entityPath);
        }

        public async Task DeleteQueue(string entityPath)
        {
            await _client.DeleteQueue(entityPath);
        }

        protected async override Task ReceiveMessages<T>(string entityPath, Predicate<T> predicate, TimeSpan timeout)
        {
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

            _ = Task.Run(() =>
            {
                if (!cancellationTokenSource.Token.WaitHandle.WaitOne(timeout))
                {
                    _client.TimedOut();
                    cancellationTokenSource.Cancel();
                }
            });

            try
            {
                while (true)
                {
                    CloudQueueMessage message = await _client.GetMessage(entityPath);

                    if (message != null)
                    {
                        QueueMessage<T> queueMessage = JsonConvert.DeserializeObject<QueueMessage<T>>(message.AsString);

                        try
                        {
                            if (predicate(queueMessage.Data))
                            {
                                break;
                            }
                        }
                        finally
                        {
                            await _client.DeleteMessage(entityPath, message);
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
            }
        }

        public async Task SendToQueue<T>(string queueName, 
            T data, 
            IDictionary<string, string> properties, 
            bool compressData = false, 
            string sessionId = null) where T : class
        {
            Guard.IsNullOrWhiteSpace(queueName, nameof(queueName));

            QueueMessage<T> queueMessage = new QueueMessage<T>
            {
                Data = data,
                UserProperties = properties
            };

            string queueMessageJson = JsonConvert.SerializeObject(queueMessage);

            CloudQueueMessage message = null;

            if (!compressData)
            {
                message = new CloudQueueMessage(queueMessageJson);
            }
            else
            {
                message = new CloudQueueMessage(CompressData(queueMessageJson));
            }

            await _client.AddMessage(queueName, message);
        }

        public async Task SendToQueueAsJson(string queueName, 
            string data, 
            IDictionary<string, string> properties, 
            bool compressData = false, 
            string sessionId = null)
        {
            Guard.IsNullOrWhiteSpace(queueName, nameof(queueName));

            string propertiesJson = properties == null ? "null" : JsonConvert.SerializeObject(properties);

            if (string.IsNullOrWhiteSpace(data))
            {
                data = "null";
            }

            string queueMessageJson = $"{{\"Data\":{data},\"UserProperties\":{propertiesJson}}}";

            CloudQueueMessage message = compressData ? new CloudQueueMessage(CompressData(queueMessageJson)) : new CloudQueueMessage(queueMessageJson);

            await _client.AddMessage(queueName, message);
        }

        public async Task SendToTopic<T>(string topicName, 
            T data, 
            IDictionary<string, string> properties, 
            bool compressData = false, 
            string sessionId = null) where T : class
        {
            await SendToQueue(topicName, data, properties, compressData);
        }

        public async Task SendToTopicAsJson(string topicName, string data, IDictionary<string, string> properties, bool compressData = false, string sessionId = null)
        {
            await SendToQueueAsJson(topicName, data, properties, compressData);
        }

        private string CompressData(string body)
        {
            byte[] zippedBytes = body.Compress();

            return Convert.ToBase64String(zippedBytes, 0, zippedBytes.Length, Base64FormattingOptions.None);
        }
    }
}
