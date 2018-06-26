using CalculateFunding.Models.Calcs;
using System.Net;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Calcs.Interfaces
{
    public interface IBuildProjectsRepository
    {
        Task<HttpStatusCode> CreateBuildProject(BuildProject buildProject);

        Task<HttpStatusCode> UpdateBuildProject(BuildProject buildProject);

	    Task<BuildProject> GetBuildProjectBySpecificationId(string specificiationId);

    }
}
