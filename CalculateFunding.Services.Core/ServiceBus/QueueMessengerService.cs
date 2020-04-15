using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core.Interfaces.ServiceBus;
using Microsoft.WindowsAzure.Storage.Queue;
using Newtonsoft.Json;
using Microsoft.WindowsAzure.Storage;
using System.Threading;
using Microsoft.Azure.ServiceBus.Core;

namespace CalculateFunding.Services.Core.ServiceBus
{
    public class QueueMessengerService : BaseMessengerService, IMessengerService, IQueueService
    {
        CloudQueueClient _queueClient;
        CloudStorageAccount _storageAccount;

        public string ServiceName { get; }

        public QueueMessengerService(string connectionString, string serviceName = null)
        {
            _storageAccount = CloudStorageAccount.Parse(connectionString);
            ServiceName = serviceName;
        }

        public async Task<(bool Ok, string Message)> IsHealthOk(string queueName)
        {
            try
            {
                CloudQueue queue = QueueClient.GetQueueReference(queueName);
                bool result = await queue.ExistsAsync();
                return (true, string.Empty);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        public async Task CreateQueue(string entityPath)
        {
            CloudQueue queue = QueueClient.GetQueueReference(entityPath);

            await queue.CreateIfNotExistsAsync();
        }

        public async Task DeleteQueue(string entityPath)
        {
            CloudQueue queue = QueueClient.GetQueueReference(entityPath);

            await queue.DeleteAsync();
        }

        protected async override Task ReceiveMessages<T>(string entityPath, Predicate<T> predicate, TimeSpan timeout)
        {
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

            CloudQueue queue = QueueClient.GetQueueReference(entityPath);

            await queue.CreateIfNotExistsAsync();

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
                    CloudQueueMessage message = await queue.GetMessageAsync();

                    if (message != null)
                    {
                        QueueMessage<T> queueMessage = JsonConvert.DeserializeObject<QueueMessage<T>>(message.AsString);

                        if (predicate(queueMessage.Data))
                        {
                            break;
                        }

                        await queue.DeleteMessageAsync(message);
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

        private CloudQueueClient QueueClient
        {
            get
            {
                if (_queueClient == null)
                {
                    _queueClient =  _storageAccount.CreateCloudQueueClient();
                }

                return _queueClient;
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

            CloudQueue queue = QueueClient.GetQueueReference(queueName);

            await queue.CreateIfNotExistsAsync();

            CloudQueueMessage message = null;

            if (!compressData)
            {
                message = new CloudQueueMessage(queueMessageJson);
            }
            else
            {
                message = new CloudQueueMessage(CompressData(queueMessageJson));
            }

            await queue.AddMessageAsync(message);
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

            CloudQueue queue = QueueClient.GetQueueReference(queueName);

            await queue.CreateIfNotExistsAsync();

            CloudQueueMessage message = compressData ? new CloudQueueMessage(CompressData(queueMessageJson)) : new CloudQueueMessage(queueMessageJson);

            await queue.AddMessageAsync(message);
        }

        public async Task SendToTopic<T>(string topicName, 
            T data, 
            IDictionary<string, string> properties, 
            bool compressData = false, 
            string sessionId = null) where T : class
        {
            await SendToQueue<T>(topicName, data, properties, compressData);
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
