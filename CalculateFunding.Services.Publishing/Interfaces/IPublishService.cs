using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IPublishService
    {
        Task PublishResults(Message message);
    }
}
