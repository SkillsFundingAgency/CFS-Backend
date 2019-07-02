using CalculateFunding.Common.CosmosDb;

namespace CalculateFunding.Services.CosmosDbScaling.Repositories
{
    public class JobsScalingRepository : CosmosDbScalingRepository
    {
        public JobsScalingRepository(ICosmosRepository cosmosRepository) : base(cosmosRepository) { }
    }
}
