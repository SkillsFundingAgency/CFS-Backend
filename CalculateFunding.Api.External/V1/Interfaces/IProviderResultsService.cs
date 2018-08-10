using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CalculateFunding.Api.External.V1.Interfaces
{
    public interface IProviderResultsService
    {
        Task<IActionResult> GetProviderResultsForAllocations(string providerId, int startYear, int endYear, string allocationLineIds, HttpRequest request);

        Task<IActionResult> GetProviderResultsForFundingStreams(string providerId, int startYear, int endYear, string fundingStreamIds, HttpRequest request);

        Task<IActionResult> GetLocalAuthorityProvidersResultsForAllocations(string laCode, int startYear, int endYear, string allocationLineIds, HttpRequest request);
    }
}
