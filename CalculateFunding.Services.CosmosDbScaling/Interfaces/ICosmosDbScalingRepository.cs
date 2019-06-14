using System.Threading.Tasks;

namespace CalculateFunding.Services.CosmosDbScaling.Interfaces
{
    public interface ICosmosDbScalingRepository
    {
        Task SetThroughput(int throughput);

        Task<int> GetCurrentThroughput();
    }
}
