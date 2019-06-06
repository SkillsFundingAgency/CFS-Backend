using CalculateFunding.Common.CosmosDb;

namespace CalculateFunding.Services.CosmosDbScaling.Repositories
{
    public class PublishedProviderResultsScalingRepository : CosmosDbScalingRepository
    {
        public PublishedProviderResultsScalingRepository(ICosmosRepository cosmosRepository) : base(cosmosRepository) { }
    }
}
