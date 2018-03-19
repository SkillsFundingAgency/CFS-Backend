using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Datasets.Interfaces
{
    public interface IDefinitionsService
    {
        Task<IActionResult> SaveDefinition(HttpRequest request);

        Task<IActionResult> GetDatasetDefinitions(HttpRequest request);

        Task<IActionResult> GetDatasetDefinitionById(HttpRequest request);

        Task<IActionResult> GetDatasetDefinitionsByIds(HttpRequest request);
    }
}
