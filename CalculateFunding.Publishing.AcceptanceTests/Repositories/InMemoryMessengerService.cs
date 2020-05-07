using CalculateFunding.Common.ServiceBus.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Publishing.AcceptanceTests.Repositories
{
    public class InMemoryMessengerService : IMessengerService
    {
        public string ServiceName => throw new NotImplementedException();

        public Task<(bool Ok, string Message)> IsHealthOk(string queueName)
        {
            throw new NotImplementedException();
        }

        public Task<T> ReceiveMessage<T>(string entityPath, Predicate<T> predicate, TimeSpan timeout) where T : class
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<T>> ReceiveMessages<T>(string entityPath, TimeSpan timeout) where T : class
        {
            throw new NotImplementedException();
        }

        public Task SendToQueue<T>(string queueName, T data, IDictionary<string, string> properties, bool compressData = false, string sessionId = null) where T : class
        {
            throw new NotImplementedException();
        }

        public Task SendToQueueAsJson(string queueName, string data, IDictionary<string, string> properties, bool compressData = false, string sessionId = null)
        {
            throw new NotImplementedException();
        }

        public Task SendToTopic<T>(string topicName, T data, IDictionary<string, string> properties, bool compressData = false, string sessionId = null) where T : class
        {
            throw new NotImplementedException();
        }

        public Task SendToTopicAsJson(string topicName, string data, IDictionary<string, string> properties, bool compressData = false, string sessionId = null)
        {
            throw new NotImplementedException();
        }
    }
}
