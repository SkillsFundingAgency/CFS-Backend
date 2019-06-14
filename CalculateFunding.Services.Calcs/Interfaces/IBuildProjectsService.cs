using System.Threading.Tasks;
using CalculateFunding.Models.Calcs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.ServiceBus;

namespace CalculateFunding.Services.Calcs.Interfaces
{
    public interface IBuildProjectsService
    {
        Task UpdateAllocations(Message message);

        Task<IActionResult> GetBuildProjectBySpecificationId(HttpRequest request);

        Task<IActionResult> UpdateBuildProjectRelationships(HttpRequest request);

        Task UpdateBuildProjectRelationships(Message message);

        Task<IActionResult> GetAssemblyBySpecificationId(string specificationId);

        Task<BuildProject> GetBuildProjectForSpecificationId(string specificationId);

        Task<IActionResult> CompileAndSaveAssembly(string specificationId);

        Task<IActionResult> GenerateAndSaveSourceProject(string specificationId, SourceCodeType sourceCodeType);
    }
}
