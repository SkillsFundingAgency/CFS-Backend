using System.Threading.Tasks;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Models;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Services.Providers.Interfaces
{
    public interface IFundingStreamProviderVersionService :  IHealthChecker
    {
        Task<IActionResult> GetCurrentProvidersForFundingStream(string fundingStreamId);

        Task<IActionResult> SetCurrentProviderVersionForFundingStream(string fundingStreamId,
            string providerVersionId);

        Task<IActionResult> GetCurrentProviderForFundingStream(string fundingStreamId,
            string providerId);

        Task<IActionResult> SearchCurrentProviderVersionsForFundingStream(string fundingStreamId,
            SearchModel search);
    }
}