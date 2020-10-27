using System.Threading.Tasks;
using CalculateFunding.Services.Jobs.Interfaces;
using Microsoft.Azure.ServiceBus;

namespace CalculateFunding.Services.Calcs.Interfaces
{
    public interface IReIndexSpecificationCalculationRelationships : IJobProcessingService
    {
    }
}