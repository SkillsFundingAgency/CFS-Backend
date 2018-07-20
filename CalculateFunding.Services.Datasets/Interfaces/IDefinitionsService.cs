using CalculateFunding.Models.Datasets.Schema;
using CalculateFunding.Repositories.Common.Search;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Datasets.Interfaces
{
    public interface IDefinitionsService
    {
        Task<IActionResult> SaveDefinition(HttpRequest request);

        Task<IActionResult> GetDatasetDefinitions(HttpRequest request);

        Task<IActionResult> GetDatasetDefinitionById(HttpRequest request);

        Task<IActionResult> GetDatasetDefinitionsByIds(HttpRequest request);

        Task<IEnumerable<IndexError>> IndexDatasetDefinition(DatasetDefinition definition);
    }
}
