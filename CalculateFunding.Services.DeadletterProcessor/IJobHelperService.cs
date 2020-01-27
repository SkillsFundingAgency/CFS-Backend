using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;

namespace CalculateFunding.Services.DeadletterProcessor
{
    public interface IJobHelperService
    {
        Task ProcessDeadLetteredMessage(Message message);
    }
}
