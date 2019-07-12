using CalculateFunding.Common.CosmosDb;

namespace CalculateFunding.Services.CosmosDbScaling.Repositories
{
    public class TestResultsScalingRepository : CosmosDbScalingRepository
    {
        public TestResultsScalingRepository(ICosmosRepository cosmosRepository) : base(cosmosRepository) { }
    }
}
