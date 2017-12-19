using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;
using Newtonsoft.Json;

namespace CalculateFunding.Functions.Common
{
    public class Messenger
    {
        private readonly Dictionary<string, TopicClient> _topicClients = new Dictionary<string, TopicClient>();
        private readonly string _connectionString;

        public Messenger(string connectionString)
        {
            _connectionString = connectionString;
        }

        public TopicClient GetTopicClient(string topicName)
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
    }
}
