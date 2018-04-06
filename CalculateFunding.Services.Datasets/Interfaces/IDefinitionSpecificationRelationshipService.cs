using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Datasets.Interfaces
{
    public interface IDefinitionSpecificationRelationshipService
    {
        Task<IActionResult> CreateRelationship(HttpRequest request);

        Task<IActionResult> GetRelationshipsBySpecificationId(HttpRequest request);

        Task<IActionResult> GetRelationshipBySpecificationIdAndName(HttpRequest request);

        Task<IActionResult> GetCurrentRelationshipsBySpecificationId(HttpRequest request);

        Task<IActionResult> GetDataSourcesByRelationshipId(HttpRequest request);

        Task<IActionResult> AssignDatasourceVersionToRelationship(HttpRequest request);
    }
}
