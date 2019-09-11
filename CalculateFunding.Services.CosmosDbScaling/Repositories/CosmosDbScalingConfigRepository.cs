using System;
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

        public async Task<CosmosDbScalingConfig> GetConfigByRepositoryType(CosmosCollectionType cosmosCollectionType)
        {
            Guard.ArgumentNotNull(cosmosCollectionType, nameof(cosmosCollectionType));

            IQueryable<CosmosDbScalingConfig> configs = _cosmosRepository.Query<CosmosDbScalingConfig>().Where(x => x.RepositoryType == cosmosCollectionType);

            return await Task.FromResult(configs.AsEnumerable().FirstOrDefault());
        }

        public async Task<IEnumerable<CosmosDbScalingConfig>> GetAllConfigs()
        {
            IQueryable<CosmosDbScalingConfig> configs = _cosmosRepository.Query<CosmosDbScalingConfig>();

            return await Task.FromResult(configs.AsEnumerable());
        }

        public async Task<CosmosDbScalingCollectionSettings> GetCollectionSettingsByRepositoryType(CosmosCollectionType cosmosCollectionType)
        {
            Guard.ArgumentNotNull(cosmosCollectionType, nameof(cosmosCollectionType));

            IQueryable<CosmosDbScalingCollectionSettings> settings = _cosmosRepository.Query<CosmosDbScalingCollectionSettings>().Where(x => x.CosmosCollectionType == cosmosCollectionType);

            return await Task.FromResult(settings?.AsEnumerable().FirstOrDefault());
        }

        public async Task<HttpStatusCode> UpdateCollectionSettings(CosmosDbScalingCollectionSettings settings)
        {
            Guard.ArgumentNotNull(settings, nameof(settings));

            return await _cosmosRepository.UpsertAsync<CosmosDbScalingCollectionSettings>(settings);
        }

        public async Task<HttpStatusCode> UpdateConfigSettings(CosmosDbScalingConfig settings)
        {
            Guard.ArgumentNotNull(settings, nameof(settings));

            return await _cosmosRepository.UpsertAsync(settings);
        }

        public async Task<IEnumerable<CosmosDbScalingCollectionSettings>> GetCollectionSettingsIncremented(int previousMinutes)
        {
            IQueryable<CosmosDbScalingCollectionSettings> settings = _cosmosRepository.Query<CosmosDbScalingCollectionSettings>()
                .Where(x => x.LastScalingIncrementDateTime <= DateTimeOffset.Now.AddMinutes(-previousMinutes));

            return await Task.FromResult(settings.AsEnumerable());
        }
    }
}
