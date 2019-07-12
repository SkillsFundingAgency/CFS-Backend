using CalculateFunding.Services.Publishing.Interfaces;
using Microsoft.Azure.ServiceBus;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing
{
    public class RefreshService : IRefreshService
    {
        public async Task RefreshResults(Message message)
        {
            await Task.CompletedTask;
        }
    }
}
