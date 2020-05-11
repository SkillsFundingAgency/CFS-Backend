using System.Threading.Tasks;
using System.Net;
using CalculateFunding.Models.Calcs;

namespace CalculateFunding.Services.Calcs.Interfaces
{
    public interface IBuildProjectsRepository
    {
        Task<HttpStatusCode> CreateBuildProject(BuildProject buildProject);

        Task<HttpStatusCode> UpdateBuildProject(BuildProject buildProject);

        Task<BuildProject> GetBuildProjectBySpecificationId(string specificiationId);
    }
}
