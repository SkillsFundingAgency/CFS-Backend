using System.Threading.Tasks;
using CalculateFunding.Services.Jobs.Interfaces;
using Microsoft.Azure.ServiceBus;

namespace CalculateFunding.Services.Policy.Interfaces
{
    public interface ITemplatesReIndexerService : IJobProcessingService
    {
    }
}
