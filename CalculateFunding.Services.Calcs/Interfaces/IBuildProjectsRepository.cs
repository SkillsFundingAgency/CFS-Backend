using CalculateFunding.Models.Calcs;
using System.Net;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Calcs.Interfaces
{
    public interface IBuildProjectsRepository
    {
        Task<BuildProject> GetBuildProjectById(string buildProjectId);

        Task<HttpStatusCode> CreateBuildProject(BuildProject buildProject);

        Task<HttpStatusCode> UpdateBuildProject(BuildProject buildProject);

	    Task<BuildProject> GetBuildProjectBySpecificationId(string specificiationId);

    }
}
