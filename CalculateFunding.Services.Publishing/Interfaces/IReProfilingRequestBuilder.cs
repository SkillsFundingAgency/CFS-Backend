using System;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Profiling.Models;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IReProfilingRequestBuilder
    {
        Task<ReProfileRequest> BuildReProfileRequest(string fundingStreamId,
            string specificationId,
            string fundingPeriodId,
            string providerId,
            string fundingLineCode,
            string profilePatternKey,
            ProfileConfigurationType configurationType,
            decimal? fundingLineTotal = null,
            bool midYear = false,
            DateTimeOffset? providerOpened = null);
    }
}