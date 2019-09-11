using CalculateFunding.Models.CosmosDbScaling;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.CosmosDbScaling.Interfaces
{
    public interface ICosmosDbScalingConfigRepository
    {
        Task<CosmosDbScalingConfig> GetConfigByRepositoryType(CosmosCollectionType cosmosRepositoryType);

        Task<IEnumerable<CosmosDbScalingConfig>> GetAllConfigs();

        Task<CosmosDbScalingCollectionSettings> GetCollectionSettingsByRepositoryType(CosmosCollectionType cosmosCollectionType);

        Task<HttpStatusCode> UpdateCollectionSettings(CosmosDbScalingCollectionSettings settings);

        Task<IEnumerable<CosmosDbScalingCollectionSettings>> GetCollectionSettingsIncremented(int previousMinutes);
        Task<HttpStatusCode> UpdateConfigSettings(CosmosDbScalingConfig cosmosDbScalingConfig);
    }
}
