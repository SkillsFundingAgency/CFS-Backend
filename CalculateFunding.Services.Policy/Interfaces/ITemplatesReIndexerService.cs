using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;

namespace CalculateFunding.Services.Policy.Interfaces
{
    public interface ITemplatesReIndexerService
    {
        Task Run(Message message);
    }
}
