using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;

namespace CalculateFunding.Services.Publishing
{
    public interface IPublishedProviderReIndexerService
    {
        Task Run(Message message);
    }
}