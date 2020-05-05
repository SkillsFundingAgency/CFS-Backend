using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IApproveService
    {
        Task ApproveResults(Message message, bool batched = false);
    }
}
