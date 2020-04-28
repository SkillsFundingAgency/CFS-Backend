using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IPublishService
    {
        Task PublishAllProviderFundingResults(Message message);
        Task PublishBatchProviderFundingResults(Message message);

    }
}
