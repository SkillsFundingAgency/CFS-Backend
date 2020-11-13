using System.Threading.Tasks;
using CalculateFunding.Services.Processing.Interfaces;
using Microsoft.Azure.ServiceBus;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IApproveService : IJobProcessingService
    {
        Task ApproveResults(Message message, bool batched = false);
    }
}
