using CalculateFunding.Services.Publishing.Interfaces;
using Microsoft.Azure.ServiceBus;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing
{
    public class PublishService : IPublishService
    {
        public async Task PublishResults(Message message)
        {
            await Task.CompletedTask;
        }
    }
}
