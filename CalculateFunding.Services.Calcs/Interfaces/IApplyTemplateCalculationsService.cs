using Microsoft.Azure.ServiceBus;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Calcs.Interfaces
{
    public interface IApplyTemplateCalculationsService
    {
        Task ApplyTemplateCalculation(Message message);
    }
}
