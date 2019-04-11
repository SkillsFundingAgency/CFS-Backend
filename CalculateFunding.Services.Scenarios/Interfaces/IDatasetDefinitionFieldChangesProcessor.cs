using Microsoft.Azure.ServiceBus;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Scenarios.Interfaces
{
    public interface IDatasetDefinitionFieldChangesProcessor
    {
        Task ProcessChanges(Message message);
    }
}
