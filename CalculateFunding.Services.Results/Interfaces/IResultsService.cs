using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;

namespace CalculateFunding.Services.Results.Interfaces
{
    public interface IResultsService
    {
     //   Task<IActionResult> CreateNewDataset(HttpRequest request);

     //   Task<IActionResult> GetDatasetByName(HttpRequest request);

     //   Task SaveNewDataset(ICloudBlob blob);

     //   Task<IActionResult> ValidateDataset(HttpRequest request);
	    //Task ProcessDataset(Message message);
	    Task UpdateProviderData(Message message);
    }
}
