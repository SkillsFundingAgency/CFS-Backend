using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core.Interfaces.ServiceBus;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Newtonsoft.Json;

namespace CalculateFunding.Services.Core.ServiceBus
{
    public class QueueMessengerService : IMessengerService
    {
        CloudQueueClient _queueClient;
        CloudStorageAccount _storageAccount;

        public QueueMessengerService(string connectionString)
        {
            _storageAccount = CloudStorageAccount.Parse(connectionString);
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

        private CloudQueueClient QueueClient
        {
            get
            {
                if (_queueClient == null)
                {
                    _queueClient = _storageAccount.CreateCloudQueueClient();
                }

                return _queueClient;
            }
        }

        public async Task SendToQueue<T>(string queueName, T data, IDictionary<string, string> properties, bool compressData = false) where T : class
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

        public async Task SendToQueueAsJson(string queueName, string data, IDictionary<string, string> properties, bool compressData = false)
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

            CloudQueueMessage message = compressData ? new CloudQueueMessage(CompressData(queueMessageJson), true) : new CloudQueueMessage(queueMessageJson);

            await queue.AddMessageAsync(message);
        }

        public async Task SendToTopic<T>(string topicName, T data, IDictionary<string, string> properties, bool compressData = false) where T : class
        {
            await SendToQueue<T>(topicName, data, properties, compressData);
        }

        public async Task SendToTopicAsJson(string topicName, string data, IDictionary<string, string> properties, bool compressData = false)
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
