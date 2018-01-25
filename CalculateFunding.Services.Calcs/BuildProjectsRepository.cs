using CalculateFunding.Models.Calcs;
using CalculateFunding.Repositories.Common.Cosmos;
using CalculateFunding.Services.Calcs.Interfaces;
using System.Net;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Calcs
{
    public class BuildProjectsRepository : IBuildProjectsRepository
    {
        private readonly CosmosRepository _cosmosRepository;

        public BuildProjectsRepository(CosmosRepository cosmosRepository)
        {
            _cosmosRepository = cosmosRepository;
        }

        public async Task<BuildProject> GetBuildProjectById(string buildProjectId)
        {
            var buildProject = await _cosmosRepository.ReadAsync<BuildProject>(buildProjectId);

            if (buildProject == null)
                return null;

            return buildProject.Content;
        }

        public Task<HttpStatusCode> CreateBuildProject(BuildProject buildProject)
        {
            return _cosmosRepository.CreateAsync(buildProject);
        }
    }
}
