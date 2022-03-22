using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Publishing.FundingManagement;
using CalculateFunding.Services.Processing.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.FundingManagement.Interfaces
{
    public interface IReleaseProvidersToChannelsService : IProcessingService
    {
        Task<IActionResult> QueueReleaseProviderVersions(string specificationId, ReleaseProvidersToChannelRequest releaseProvidersToChannelRequest, Reference author, string correlationId);

        Task<IActionResult> QueueRelease(string specificationId, ReleaseProvidersToChannelRequest releaseProvidersToChannelRequest, Reference author, string correlationId);

        Task ReleaseProviderVersions(SpecificationSummary specification,
                                     ReleaseProvidersToChannelRequest releaseProvidersToChannelRequest,
                                     string jobId,
                                     string correlationId,
                                     Reference author);
    }
}