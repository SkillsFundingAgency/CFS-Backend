using CalculateFunding.Models.Datasets;
using Microsoft.Azure.ServiceBus;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Datasets.Interfaces
{
    public interface IDatasetDefinitionNameChangeProcessor
    {
        Task ProcessChanges(Message message);
    }
}
