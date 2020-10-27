using System.Threading.Tasks;
using CalculateFunding.Services.Jobs.Interfaces;
using Microsoft.Azure.ServiceBus;

namespace CalculateFunding.Services.Publishing
{
    public interface IPublishedProviderReIndexerService : IJobProcessingService
    {
    }
}