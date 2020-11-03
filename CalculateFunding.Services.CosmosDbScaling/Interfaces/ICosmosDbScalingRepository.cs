using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;

namespace CalculateFunding.Services.CosmosDbScaling.Interfaces
{
    public interface ICosmosDbScalingRepository
    {
        Task<ThroughputResponse> SetThroughput(int throughput);

        Task<int?> GetCurrentThroughput();

        Task<int?> GetMinimumThroughput();
    }
}
