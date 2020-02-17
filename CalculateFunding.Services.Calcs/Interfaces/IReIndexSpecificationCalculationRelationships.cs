using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;

namespace CalculateFunding.Services.Calcs.Interfaces
{
    public interface IReIndexSpecificationCalculationRelationships
    {
        Task Run(Message message);
    }
}