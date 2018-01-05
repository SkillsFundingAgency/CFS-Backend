using System.Threading.Tasks;

namespace CalculateFunding.Functions.Common
{
    public interface IMessenger
    {
        Task SendAsync<T>(string topicName, T command);
    }
}