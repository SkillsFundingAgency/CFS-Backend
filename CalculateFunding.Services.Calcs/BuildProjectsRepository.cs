using System.Linq;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Repositories.Common.Cosmos;
using CalculateFunding.Services.Calcs.Interfaces;
using System.Net;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace CalculateFunding.Services.Calcs
{
    public class BuildProjectsRepository : IBuildProjectsRepository
    {
        private readonly CosmosRepository _cosmosRepository;

        public BuildProjectsRepository(CosmosRepository cosmosRepository)
        {
            _cosmosRepository = cosmosRepository;
        }

        // No longer used directly, lookup is by specification ID instead
        //public async Task<BuildProject> GetBuildProjectById(string buildProjectId)
        //{
        //    var buildProject = await _cosmosRepository.ReadAsync<BuildProject>(buildProjectId);

        //    if (buildProject == null)
        //        return null;

        //    return buildProject.Content;
        //}

	    public Task<BuildProject> GetBuildProjectBySpecificationId(string specificiationId)
	    {
		    IEnumerable<BuildProject> buildProjects = _cosmosRepository.Query<BuildProject>().Where(x => x.SpecificationId == specificiationId).ToList();

            return Task.FromResult(buildProjects.FirstOrDefault());
	    }

		public Task<HttpStatusCode> CreateBuildProject(BuildProject buildProject)
        {
            return _cosmosRepository.CreateAsync(buildProject);
        }

        public Task<HttpStatusCode> UpdateBuildProject(BuildProject buildProject)
        {
            return _cosmosRepository.UpdateAsync(buildProject);
        }
    }
}
