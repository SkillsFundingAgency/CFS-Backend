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

            IEnumerable<CosmosDbScalingConfig> configs = (await _cosmosRepository.Query<CosmosDbScalingConfig>(x => x.Content.RepositoryType == cosmosCollectionType));

            return configs.FirstOrDefault();
        }

        public async Task<IEnumerable<CosmosDbScalingConfig>> GetAllConfigs()
        {
            return await _cosmosRepository.Query<CosmosDbScalingConfig>();
        }

        public async Task<CosmosDbScalingCollectionSettings> GetCollectionSettingsByRepositoryType(CosmosCollectionType cosmosCollectionType)
        {
            Guard.ArgumentNotNull(cosmosCollectionType, nameof(cosmosCollectionType));

            IEnumerable<CosmosDbScalingCollectionSettings> settings = (await _cosmosRepository.Query<CosmosDbScalingCollectionSettings>(x => x.Content.CosmosCollectionType == cosmosCollectionType));

            return settings?.AsEnumerable().FirstOrDefault();
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
            IEnumerable<CosmosDbScalingCollectionSettings> settings = await _cosmosRepository.Query<CosmosDbScalingCollectionSettings>(x => x.Content.LastScalingIncrementDateTime <= DateTimeOffset.Now.AddMinutes(-previousMinutes));
        
            return settings.AsEnumerable();
        }
    }
}
