using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Api.External.V2.Interfaces
{
    public interface IProviderResultsService
    {
        Task<IActionResult> GetProviderResultsForAllocations(string providerId, int startYear, int endYear, string allocationLineIds, HttpRequest request);

        Task<IActionResult> GetProviderResultsForFundingStreams(string providerId, int startYear, int endYear, string fundingStreamIds, HttpRequest request);

        Task<IActionResult> GetLocalAuthorityProvidersResultsForAllocations(string laCode, int startYear, int endYear, string allocationLineIds, HttpRequest request);
    }
}
