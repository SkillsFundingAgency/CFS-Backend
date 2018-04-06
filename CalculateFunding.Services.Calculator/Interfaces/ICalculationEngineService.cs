using Microsoft.Azure.ServiceBus;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Calculator.Interfaces
{
    public interface ICalculationEngineService
    {
        Task GenerateAllocations(Message message);
    }
}
