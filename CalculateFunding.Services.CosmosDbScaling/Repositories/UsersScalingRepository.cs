using CalculateFunding.Common.CosmosDb;

namespace CalculateFunding.Services.CosmosDbScaling.Repositories
{
    public class UsersScalingRepository : CosmosDbScalingRepository
    {
        public UsersScalingRepository(ICosmosRepository cosmosRepository) : base(cosmosRepository) { }
    }
}
