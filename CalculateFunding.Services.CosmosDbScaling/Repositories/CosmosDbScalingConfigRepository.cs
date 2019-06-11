using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.CosmosDbScaling;
using CalculateFunding.Services.CosmosDbScaling.Interfaces;

namespace CalculateFunding.Services.CosmosDbScaling.Repositories
{
    public class CosmosDbScalingConfigRepository : ICosmosDbScalingConfigRepository
    {
        private readonly ICosmosRepository _cosmosRepository;

        public CosmosDbScalingConfigRepository(ICosmosRepository cosmosRepository)
        {
            _cosmosRepository = cosmosRepository;
        }

        public async Task<CosmosDbScalingConfig> GetConfigByRepositoryType(CosmosRepositoryType cosmosRepositoryType)
        {
            IQueryable<CosmosDbScalingConfig> configs = _cosmosRepository.Query<CosmosDbScalingConfig>().Where(x => x.RepositoryType == cosmosRepositoryType);

            return await Task.FromResult(configs.AsEnumerable().FirstOrDefault());
        }

        public async Task<IEnumerable<CosmosDbScalingConfig>> GetAllConfigs()
        {
            IQueryable<CosmosDbScalingConfig> configs = _cosmosRepository.Query<CosmosDbScalingConfig>();

            return await Task.FromResult(configs.AsEnumerable());
        }

        public async Task<HttpStatusCode> UpdateCurrentRequestUnits(CosmosDbScalingConfig config)
        {
            Guard.ArgumentNotNull(config, nameof(config));

            return await _cosmosRepository.UpsertAsync<CosmosDbScalingConfig>(config);
        }
    }
}
