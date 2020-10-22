using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Profiling.Models;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IReProfilingRequestBuilder
    {
        Task<ReProfileRequest> BuildReProfileRequest(string specificationId,
            string fundingStreamId,
            string fundingPeriodId,
            string providerId,
            string fundingLineCode,
            string profilePatternKey,
            ProfileConfigurationType configurationType,
            decimal? fundingLineTotal = null);
    }
}