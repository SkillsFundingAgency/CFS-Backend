using CalculateFunding.Services.Jobs.Interfaces;
using Microsoft.Azure.ServiceBus;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Calcs.Interfaces
{
    public interface IApplyTemplateCalculationsService : IJobProcessingService
    {
    }
}
