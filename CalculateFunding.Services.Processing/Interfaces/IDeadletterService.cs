using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;

namespace CalculateFunding.Services.Processing.Interfaces
{
    public interface IDeadletterService
    {
        Task Process(Message message);
    }
}
