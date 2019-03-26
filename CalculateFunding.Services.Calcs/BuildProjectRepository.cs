using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Services.Calcs.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Calcs
{
    public class BuildProjectsRepository : IBuildProjectsRepository, IHealthChecker
    {
        private readonly CosmosRepository _cosmosRepository;

        public BuildProjectsRepository(CosmosRepository cosmosRepository)
        {
            _cosmosRepository = cosmosRepository;
        }

        public async Task<ServiceHealth> IsHealthOk()
        {
            ServiceHealth health = new ServiceHealth();

            var cosmosHealth = await _cosmosRepository.IsHealthOk();

            health.Name = this.GetType().Name;
            health.Dependencies.Add(new DependencyHealth { HealthOk = cosmosHealth.Ok, DependencyName = typeof(CosmosRepository).Name, Message = cosmosHealth.Message });

            return health;
        }

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
            return _cosmosRepository.UpsertAsync(buildProject);
        }
    }
}
