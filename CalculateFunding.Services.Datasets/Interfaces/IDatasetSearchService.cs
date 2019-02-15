using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Datasets.Interfaces
{
    public interface IDatasetSearchService
    {
        Task<IActionResult> SearchDatasets(HttpRequest request);

	    Task<IActionResult> SearchDatasetVersion(HttpRequest request);

    }
}
