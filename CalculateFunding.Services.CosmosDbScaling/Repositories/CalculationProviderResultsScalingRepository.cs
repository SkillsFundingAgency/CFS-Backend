using CalculateFunding.Common.CosmosDb;

namespace CalculateFunding.Services.CosmosDbScaling.Repositories
{
    public class CalculationProviderResultsScalingRepository : CosmosDbScalingRepository
    {
        public CalculationProviderResultsScalingRepository(ICosmosRepository cosmosRepository) : base(cosmosRepository){}
    }
}
