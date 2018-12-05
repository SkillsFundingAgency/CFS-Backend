using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;

namespace CalculateFunding.Services.Notifications.Interfaces
{
    public interface INotificationService
    {
        Task OnNotificationEvent(Message message, IAsyncCollector<SignalRMessage> signalRMessages);
    }
}
