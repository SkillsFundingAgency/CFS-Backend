using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;

namespace CalculateFunding.Services.Results.Interfaces
{
    public interface IProviderResultsCsvGeneratorService
    {
        Task Run(Message message);
    }
}