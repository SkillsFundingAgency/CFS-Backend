using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using CalculateFunding.Services.Core.Interfaces.ServiceBus;
using CalculateFunding.Services.Core.Options;
using Microsoft.Azure.ServiceBus;
using Newtonsoft.Json;


namespace CalculateFunding.Services.Core
{
    public class MessengerService : IMessengerService
    {
        private readonly Dictionary<string, TopicClient> _topicClients = new Dictionary<string, TopicClient>();
        private readonly string _connectionString;
       
        public MessengerService(ServiceBusSettings settings)
        {
            _connectionString = settings.ServiceBusConnectionString;
        }

        TopicClient GetTopicClient(string topicName)
        {
            if (!_topicClients.TryGetValue(topicName, out var topicClient))
            {
                topicClient = new TopicClient(_connectionString, topicName);
                _topicClients.Add(topicName, topicClient);
            }
            return topicClient;
        }

        public async Task SendAsync<T>(string topicName, T command)
        {
            var topicClient = GetTopicClient(topicName);
            
            var json = JsonConvert.SerializeObject(command);
            await topicClient.SendAsync(new Message(Encoding.UTF8.GetBytes(json)));
        }

        async public Task SendAsync<T>(string topicName, string subscriptionName, T data, IDictionary<string, string> properties)
        {
            var topicClient = GetTopicClient(topicName);

            var json = JsonConvert.SerializeObject(data);

            Message message = new Message(Encoding.UTF8.GetBytes(json))
            {
                Label = subscriptionName
            };

            foreach (var property in properties)
                message.UserProperties.Add(property.Key, property.Value);
           
            await topicClient.SendAsync(message);
        }
    }
}
