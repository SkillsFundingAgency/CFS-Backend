using System;
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
        private readonly Dictionary<string, QueueClient> _queueClients = new Dictionary<string, QueueClient>();
        private readonly string _connectionString;

        private object queueClientLock = new object();

        public MessengerService(ServiceBusSettings settings)
        {
            _connectionString = settings.ConnectionString;
        }

        QueueClient GetQueueClient(string queueName)
        {
            if (!_queueClients.TryGetValue(queueName, out var queueClient))
            {
                queueClient = new QueueClient(_connectionString, queueName);
                if (!_queueClients.ContainsKey(queueName))
                {
                    lock (queueClientLock)
                    {
                        _queueClients.Add(queueName, queueClient);
                    }
                }
            }
            return new QueueClient(_connectionString, queueName);
        }

        public async Task SendToQueue<T>(string queueName, T data, IDictionary<string, string> properties)
        {
            var queueClient = GetQueueClient(queueName);

            var json = JsonConvert.SerializeObject(data);

            Message message = new Message(Encoding.UTF8.GetBytes(json));

            foreach (var property in properties)
                message.UserProperties.Add(property.Key, property.Value);

            await RetryAgent.DoAsync(() => queueClient.SendAsync(message));
        }
    }
}
