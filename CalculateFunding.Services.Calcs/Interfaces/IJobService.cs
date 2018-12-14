using Microsoft.Azure.ServiceBus;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Calcs.Interfaces
{
    public interface IJobService
    {
        Task CreateInstructAllocationJob(Message message);
    }
}
