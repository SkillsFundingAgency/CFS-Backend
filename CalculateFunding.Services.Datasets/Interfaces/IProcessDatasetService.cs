using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.ServiceBus;

namespace CalculateFunding.Services.Datasets.Interfaces
{
    public interface IProcessDatasetService
    {
        Task ProcessDataset(Message message);

        Task<IActionResult> GetDatasetAggregationsBySpecificationId(string specificationId);
    }
}
