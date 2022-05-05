using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using CalculateFunding.Services.Publishing.Models;
using System.Collections.Generic;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IPublishedProviderStatusService
    {
        Task<IActionResult> GetProviderStatusCounts(string specificationId,
            string providerType,
            string localAuthority,
            IEnumerable<string> statuses,
            bool? isIndicative = null,
            string monthYearOpened = null);

        Task<IActionResult> GetProviderBatchCountForApproval(PublishedProviderIdsRequest providerIds, string specificationId);
        
        Task<IActionResult> GetProviderBatchCountForRelease(PublishedProviderIdsRequest providerIds, string specificationId);

        Task<IActionResult> GetProviderDataForBatchApprovalAsCsv(PublishedProviderIdsRequest providerIds, string specificationId);

        Task<IActionResult> GetProviderDataForBatchReleaseAsCsv(PublishedProviderIdsRequest providerIds, string specificationId);

        Task<IActionResult> GetProviderDataForAllApprovalAsCsv(string specificationId);

        Task<IActionResult> GetProviderDataForAllReleaseAsCsv(string specificationId);

        Task<IActionResult> GetApprovedPublishedProviderReleaseFundingSummary(ReleaseFundingPublishProvidersRequest request, string specificationId);

        Task<IActionResult> GetPublishedProviderTransactions(string specificationId, string providerId);
    }
}
