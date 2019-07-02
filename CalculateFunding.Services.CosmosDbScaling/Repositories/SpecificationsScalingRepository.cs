using CalculateFunding.Common.CosmosDb;

namespace CalculateFunding.Services.CosmosDbScaling.Repositories
{
    public class SpecificationsScalingRepository : CosmosDbScalingRepository
    {
        public SpecificationsScalingRepository(ICosmosRepository cosmosRepository) : base(cosmosRepository) { }
    }
}
