using CalculateFunding.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Datasets.Interfaces
{
    public interface IDatasetDefinitionSearchService
    {
        Task<IActionResult> SearchDatasetDefinitions(SearchModel searchModel);
    }
}
