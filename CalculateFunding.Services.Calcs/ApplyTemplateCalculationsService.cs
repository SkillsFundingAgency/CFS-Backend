using CalculateFunding.Services.Calcs.Interfaces;
using Microsoft.Azure.ServiceBus;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Calcs
{
    public class ApplyTemplateCalculationsService : IApplyTemplateCalculationsService
    {
        public async Task ApplyTemplateCalculation(Message message)
        {
            await Task.CompletedTask;
        }
    }
}
