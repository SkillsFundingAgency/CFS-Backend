using System.Threading.Tasks;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Services.CosmosDbScaling.Interfaces;
using Microsoft.Azure.Cosmos;

namespace CalculateFunding.Services.CosmosDbScaling.Repositories
{
    public abstract class CosmosDbScalingRepository : ICosmosDbScalingRepository
    {
        private readonly ICosmosRepository _cosmosRepository;

        public CosmosDbScalingRepository(ICosmosRepository cosmosRepository)
        {
            _cosmosRepository = cosmosRepository;
        }

        public async Task<ThroughputResponse> SetThroughput(int throughput)
        {
            return await _cosmosRepository.SetThroughput(throughput);
        }

        public async Task<int?> GetCurrentThroughput()
        {
            return await _cosmosRepository.GetThroughput();
        }
    }
}
