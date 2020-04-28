using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IApproveService
    {
        Task ApproveAllResults(Message message);
        Task ApproveBatchResults(Message message);

    }
}
