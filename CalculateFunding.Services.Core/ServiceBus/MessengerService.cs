using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Interfaces.ServiceBus;
using CalculateFunding.Services.Core.Options;
using Microsoft.Azure.ServiceBus;
using Newtonsoft.Json;


namespace CalculateFunding.Services.Core.ServiceBus
{
    public class MessengerService : IMessengerService
    {
        private static readonly ConcurrentDictionary<string, QueueClient> _queueClients = new ConcurrentDictionary<string, QueueClient>();
        private readonly string _connectionString;

        private object queueClientLock = new object();

        public MessengerService(ServiceBusSettings settings)
        {
            _connectionString = settings.ConnectionString;
        }

        QueueClient GetQueueClient(string queueName)
        {
            return _queueClients.GetOrAdd(queueName, (key) => {
                return new QueueClient(_connectionString, key, ReceiveMode.PeekLock, RetryExponential.Default);
            });
        }

        public async Task SendToQueue<T>(string queueName, T data, IDictionary<string, string> properties)
        {
            var queueClient = GetQueueClient(queueName);

            var json = JsonConvert.SerializeObject(data);

            Message message = new Message(Encoding.UTF8.GetBytes(json));
            message.PartitionKey = Guid.NewGuid().ToString();

            foreach (var property in properties)
                message.UserProperties.Add(property.Key, property.Value);

            await queueClient.SendAsync(message);
        }
    }
}
