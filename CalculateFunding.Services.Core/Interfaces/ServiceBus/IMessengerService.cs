using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Core.Interfaces.ServiceBus
{
    public interface IMessengerService
    {
        Task SendAsync<T>(string hubName, T data, IDictionary<string, string> properties);
        Task SendBatchAsync<T>(string hubName, IEnumerable<T> data, IDictionary<string, string> properties);
    }
}