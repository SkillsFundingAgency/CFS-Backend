using System.Threading.Tasks;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Services.Processing.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.ServiceBus;

namespace CalculateFunding.Services.Calcs.Interfaces
{
    public interface IBuildProjectsService : IJobProcessingService
    {
        Task<IActionResult> GetBuildProjectBySpecificationId(string specificationId);

        Task<IActionResult> UpdateBuildProjectRelationships(string specificationId, DatasetRelationshipSummary relationship);

        Task UpdateBuildProjectRelationships(Message message);

        Task<IActionResult> GetAssemblyBySpecificationId(string specificationId);

        Task<BuildProject> GetBuildProjectForSpecificationId(string specificationId);

        Task<IActionResult> CompileAndSaveAssembly(string specificationId);

        Task<IActionResult> GenerateAndSaveSourceProject(string specificationId, SourceCodeType sourceCodeType);
    }
}
