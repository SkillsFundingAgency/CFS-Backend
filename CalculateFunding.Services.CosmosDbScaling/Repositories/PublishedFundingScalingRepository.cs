using CalculateFunding.Common.CosmosDb;

namespace CalculateFunding.Services.CosmosDbScaling.Repositories
{
    public class PublishedFundingScalingRepository : CosmosDbScalingRepository
    {
        public PublishedFundingScalingRepository(ICosmosRepository cosmosRepository) : base(cosmosRepository) { }
    }
}
