using System.Threading.Tasks;
using CalculateFunding.Services.Core.Interfaces.Services;
using Microsoft.Azure.ServiceBus;

namespace CalculateFunding.Services.Results.Interfaces
{
    public interface IProviderResultsCsvGeneratorService : IProcessingService
    {
    }
}