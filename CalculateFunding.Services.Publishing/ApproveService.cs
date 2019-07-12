using CalculateFunding.Services.Publishing.Interfaces;
using Microsoft.Azure.ServiceBus;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing
{
    public class ApproveService : IApproveService
    {
        public async Task ApproveResults(Message message)
        {
            await Task.CompletedTask;
        }
    }
}
