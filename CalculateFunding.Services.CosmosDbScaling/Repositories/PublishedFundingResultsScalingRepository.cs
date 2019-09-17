using CalculateFunding.Common.CosmosDb;

namespace CalculateFunding.Services.CosmosDbScaling.Repositories
{
    public class PublishedFundingResultsScalingRepository : CosmosDbScalingRepository
    {
        public PublishedFundingResultsScalingRepository(ICosmosRepository cosmosRepository) : base(cosmosRepository) { }
    }
}
