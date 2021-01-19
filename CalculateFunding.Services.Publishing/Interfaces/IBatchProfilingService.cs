using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IBatchProfilingService
    {
        Task ProfileBatches(IBatchProfilingContext batchProfilingContext);
    }
}