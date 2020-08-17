using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IPublishService
    {
        Task PublishProviderFundingResults(Message message, bool batched = false, int deliveryCount = 1);
    }
}
