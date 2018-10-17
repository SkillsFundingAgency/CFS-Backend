using System.Threading.Tasks;
using CalculateFunding.Models.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.ServiceBus;

namespace CalculateFunding.Services.Results.Interfaces
{
    public interface IPublishedResultsService
    {
        Task PublishProviderResults(Message message);
        Task<IActionResult> GetPublishedProviderResultsBySpecificationId(HttpRequest request);
        Task<IActionResult> GetPublishedProviderResultsByFundingPeriodIdAndSpecificationIdAndFundingStreamId(HttpRequest request);
        Task<IActionResult> GetConfirmationDetailsForApprovePublishProviderResults(HttpRequest request);
        Task<IActionResult> UpdatePublishedAllocationLineResultsStatus(HttpRequest request);
        Task<PublishedProviderResult> GetPublishedProviderResultByAllocationResultId(string allocationResultId, int? version = null);
        Task<PublishedProviderResultWithHistory> GetPublishedProviderResultWithHistoryByAllocationResultId(string allocationResultId);
        Task<IActionResult> ReIndexAllocationNotificationFeeds();
        Task FetchProviderProfile(Message message);
    }
}
