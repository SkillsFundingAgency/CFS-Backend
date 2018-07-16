using CalculateFunding.Services.Core.Interfaces.ServiceBus;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

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

       private CloudQueueClient QueueClient
       {
            get
            {
                if(_queueClient == null)
                {
                    _queueClient = _storageAccount.CreateCloudQueueClient();
                }

                return _queueClient;
            }
       }

        public async Task SendToQueue<T>(string queueName, T data, IDictionary<string, string> properties) where T : class
        {
            QueueMessage<T> queueMessage = new QueueMessage<T>
            {
                Data = data,
                UserProperties = properties
            };

            string queueMessageJson = JsonConvert.SerializeObject(queueMessage);

            CloudQueue queue = QueueClient.GetQueueReference(queueName);

            await queue.CreateIfNotExistsAsync();

            CloudQueueMessage message = new CloudQueueMessage(queueMessageJson);

            await queue.AddMessageAsync(message);
        }

        public Task SendToTopic<T>(string topicName, T data, IDictionary<string, string> properties) where T : class
        {
            return SendToQueue<T>(topicName, data, properties);
        }
    }
}
