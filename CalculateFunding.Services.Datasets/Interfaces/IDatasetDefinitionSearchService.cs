using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Datasets.Interfaces
{
    public interface IDatasetDefinitionSearchService
    {
        Task<IActionResult> SearchDatasetDefinitions(HttpRequest request);
    }
}
