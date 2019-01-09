using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Core.Interfaces.ServiceBus
{
    public interface IMessengerService
    {
        Task<(bool Ok, string Message)> IsHealthOk(string queueName);

        Task SendToQueue<T>(string queueName, T data, IDictionary<string, string> properties) where T : class;

        Task SendToQueueAsJson(string queueName, string data, IDictionary<string, string> properties);

        Task SendToTopic<T>(string topicName, T data, IDictionary<string, string> properties) where T : class;

        Task SendToTopicAsJson(string topicName, string data, IDictionary<string, string> properties);
    }
}