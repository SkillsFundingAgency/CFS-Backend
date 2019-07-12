using CalculateFunding.Common.CosmosDb;

namespace CalculateFunding.Services.CosmosDbScaling.Repositories
{
    public class CalculationsScalingRepository : CosmosDbScalingRepository
    {
        public CalculationsScalingRepository(ICosmosRepository cosmosRepository) : base(cosmosRepository) { }
    }
}
