using CalculateFunding.Common.CosmosDb;

namespace CalculateFunding.Services.CosmosDbScaling.Repositories
{
    public class TestsScalingRepository : CosmosDbScalingRepository
    {
        public TestsScalingRepository(ICosmosRepository cosmosRepository) : base(cosmosRepository) { }
    }
}
