using CalculateFunding.Common.ApiClient.Profiling.Models;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IBatchProfilingContext
    {
        void InitialiseItems(int pageSize,
            int batchSize);

        void ReconcileBatchProfilingResponse(BatchProfilingResponseModel response);
        
        BatchProfilingRequestModel[] NextPage();
        
        bool HasPages { get; }
    }
}