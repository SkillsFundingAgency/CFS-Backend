using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Core.Interfaces.ServiceBus
{
    public interface IMessengerService
    {
        Task SendAsync<T>(string topicName, T command);

        Task SendAsync<T>(string topicName, string subscriptionName, T data, IDictionary<string, string> properties);
    }
}