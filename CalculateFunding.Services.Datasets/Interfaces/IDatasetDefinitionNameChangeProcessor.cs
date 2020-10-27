using CalculateFunding.Models.Datasets;
using CalculateFunding.Services.Core.Interfaces.Services;
using Microsoft.Azure.ServiceBus;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Datasets.Interfaces
{
    public interface IDatasetDefinitionNameChangeProcessor : IProcessingService
    {
    }
}
