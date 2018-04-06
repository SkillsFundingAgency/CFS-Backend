using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Core.Interfaces.ServiceBus
{
    public interface IMessengerService
    {
        Task SendToQueue<T>(string topicName, T data, IDictionary<string, string> properties);
    }
}