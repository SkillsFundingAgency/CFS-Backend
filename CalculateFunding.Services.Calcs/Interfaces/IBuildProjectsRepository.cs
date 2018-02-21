using CalculateFunding.Models.Calcs;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
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
