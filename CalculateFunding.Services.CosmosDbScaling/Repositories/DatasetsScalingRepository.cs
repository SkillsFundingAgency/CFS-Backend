using CalculateFunding.Common.CosmosDb;

namespace CalculateFunding.Services.CosmosDbScaling.Repositories
{
    public class DatasetsScalingRepository : CosmosDbScalingRepository
    {
        public DatasetsScalingRepository(ICosmosRepository cosmosRepository) : base(cosmosRepository) { }
    }
}
