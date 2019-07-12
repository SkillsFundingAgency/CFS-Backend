using Microsoft.Azure.ServiceBus;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IApproveService
    {
        Task ApproveResults(Message message);
    }
}
