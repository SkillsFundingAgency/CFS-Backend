using CalculateFunding.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Datasets.Interfaces
{
    public interface IDatasetSearchService
    {
        Task<IActionResult> SearchDatasets(SearchModel searchModel);

	    Task<IActionResult> SearchDatasetVersion(SearchModel searchModel);

    }
}
