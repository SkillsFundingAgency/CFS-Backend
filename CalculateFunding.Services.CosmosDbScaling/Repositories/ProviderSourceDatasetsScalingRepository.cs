using CalculateFunding.Common.CosmosDb;

namespace CalculateFunding.Services.CosmosDbScaling.Repositories
{
    public class ProviderSourceDatasetsScalingRepository : CosmosDbScalingRepository
    {
        public ProviderSourceDatasetsScalingRepository(ICosmosRepository cosmosRepository) : base(cosmosRepository) { }
    }
}
