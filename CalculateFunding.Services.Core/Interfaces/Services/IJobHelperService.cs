using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;

namespace CalculateFunding.Services.Core.Interfaces.Services
{
    public interface IJobHelperService
    {
        Task ProcessDeadLetteredMessage(Message message);
    }
}
