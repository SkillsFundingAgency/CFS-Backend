using CalculateFunding.Services.Jobs.Interfaces;
using Microsoft.Azure.ServiceBus;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IPublishedProviderEstateCsvGenerator : IJobProcessingService
    {
    }
}
