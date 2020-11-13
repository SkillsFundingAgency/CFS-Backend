using System.Threading.Tasks;
using CalculateFunding.Services.Processing.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.ServiceBus;

namespace CalculateFunding.Services.Datasets.Interfaces
{
    public interface IProcessDatasetService : IJobProcessingService
    {
        Task<IActionResult> GetDatasetAggregationsBySpecificationId(string specificationId);

        Task MapFdzDatasets(Message message);
    }
}
