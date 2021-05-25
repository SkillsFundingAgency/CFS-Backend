using CalculateFunding.Common.Models;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Models.Datasets.ViewModels;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace CalculateFunding.Services.Datasets.Interfaces
{
    public interface IDefinitionSpecificationRelationshipService
    {
        Task<IActionResult> CreateRelationship(CreateDefinitionSpecificationRelationshipModel createDefinitionSpecificationRelationshipModel, Reference user, string correlationId);

        Task<IEnumerable<DatasetSpecificationRelationshipViewModel>> GetRelationshipsBySpecificationId(string specificationId);

        Task<IActionResult> GetRelationshipsBySpecificationIdResult(string specificationId);

        Task<IActionResult> GetRelationshipBySpecificationIdAndName(string specificationId, string name);

        Task<IActionResult> GetCurrentRelationshipsBySpecificationId(string specificationId);

        Task<IActionResult> GetDataSourcesByRelationshipId(string relationshipId);

        Task<IActionResult> AssignDatasourceVersionToRelationship(AssignDatasourceModel assignDatasourceModel, Reference user, string correlationId);

        Task<IActionResult> GetCurrentDatasetRelationshipFieldsBySpecificationId(string specificationId);

        Task<IActionResult> GetSpecificationIdsForRelationshipDefinitionId(string datasetDefinitionId);

        Task UpdateRelationshipDatasetDefinitionName(Reference datasetDefinitionReference);

        Task<IActionResult> GetCurrentRelationshipsBySpecificationIdAndDatasetDefinitionId(string specificationId, string datasetDefinitionId);
    }
}
