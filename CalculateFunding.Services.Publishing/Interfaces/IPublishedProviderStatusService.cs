using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Models;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IPublishedProviderStatusService
    {
        Task<IActionResult> GetProviderStatusCounts(string specificationId, string providerType, string localAuthority, string status);

        Task<IActionResult> GetProviderBatchCountForApproval(PublishedProviderIdsRequest providerIds,
            string specificationId);
        
        Task<IActionResult> GetProviderBatchCountForRelease(PublishedProviderIdsRequest providerIds,
            string specificationId);
    }
}
