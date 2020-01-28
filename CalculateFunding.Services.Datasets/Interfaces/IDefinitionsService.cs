using CalculateFunding.Common.Models;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Models.Datasets.Schema;
using CalculateFunding.Repositories.Common.Search;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Datasets.Interfaces
{
    public interface IDefinitionsService
    {
        Task<IActionResult> SaveDefinition(string yaml, string yamlFilename, Reference user, string correlationId);

        Task<IActionResult> GetDatasetDefinitions();

        Task<IActionResult> GetDatasetDefinitionById(string datasetDefinitionId);

        Task<IActionResult> GetDatasetDefinitionsByIds(IEnumerable<string> definitionIds);

        Task<IEnumerable<IndexError>> IndexDatasetDefinition(DatasetDefinition definition);

        Task<IActionResult> GetDatasetSchemaSasUrl(DatasetSchemaSasUrlRequestModel datasetSchemaSasUrlRequestModel);
    }
}
