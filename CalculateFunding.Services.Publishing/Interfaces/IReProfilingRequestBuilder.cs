using System;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Profiling.Models;
using CalculateFunding.Models.Publishing;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IReProfilingRequestBuilder
    {
        Task<ReProfileRequest> BuildReProfileRequest(string fundingLineCode,
            string profilePatternKey,
            PublishedProviderVersion publishedProviderVersion,
            ProfileConfigurationType configurationType,
            decimal? fundingLineTotal = null,
            Provider currentProvider = null,
            bool midYear = false);
    }
}