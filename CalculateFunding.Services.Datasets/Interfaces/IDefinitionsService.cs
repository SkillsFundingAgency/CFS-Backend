using CalculateFunding.Common.Models;
using CalculateFunding.Models.Datasets;
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

        Task<IActionResult> GetDatasetDefinitionsByFundingStreamId(string fundingStreamId);

        Task<IActionResult> GetDatasetSchemaSasUrl(DatasetSchemaSasUrlRequestModel datasetSchemaSasUrlRequestModel);
    }
}
