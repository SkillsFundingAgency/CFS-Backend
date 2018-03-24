using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.EventHubs;

namespace CalculateFunding.Services.Calcs.Interfaces
{
    public interface IBuildProjectsService
    {
        Task UpdateAllocations(EventData message);
        Task UpdateBuildProjectRelationships(EventData message);
        Task<IActionResult> GetBuildProjectBySpecificationId(HttpRequest request);
        Task<IActionResult> UpdateBuildProjectRelationships(HttpRequest request);
    }
}
