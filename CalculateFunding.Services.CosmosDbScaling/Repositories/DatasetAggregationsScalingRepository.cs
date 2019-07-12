using CalculateFunding.Common.CosmosDb;

namespace CalculateFunding.Services.CosmosDbScaling.Repositories
{
    public class DatasetAggregationsScalingRepository : CosmosDbScalingRepository
    {
        public DatasetAggregationsScalingRepository(ICosmosRepository cosmosRepository) : base(cosmosRepository) { }
    }
}
