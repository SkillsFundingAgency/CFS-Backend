using System.Threading.Tasks;
using CalculateFunding.Services.Processing.Interfaces;
using Microsoft.Azure.ServiceBus;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IPublishService : IJobProcessingService
    {
        Task PublishProviderFundingResults(Message message, bool batched = false);
    }
}
