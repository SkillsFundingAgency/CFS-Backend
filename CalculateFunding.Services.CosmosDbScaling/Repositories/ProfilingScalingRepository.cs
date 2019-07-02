using CalculateFunding.Common.CosmosDb;

namespace CalculateFunding.Services.CosmosDbScaling.Repositories
{
    public class ProfilingScalingRepository : CosmosDbScalingRepository
    {
        public ProfilingScalingRepository(ICosmosRepository cosmosRepository) : base(cosmosRepository) { }
    }
}
