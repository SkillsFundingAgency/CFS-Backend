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

        Task PublishProviderResultsWithVariations(Message message);

        Task<IActionResult> GetPublishedProviderResultsBySpecificationId(HttpRequest request);

        Task<IActionResult> GetPublishedProviderResultsByFundingPeriodIdAndSpecificationIdAndFundingStreamId(HttpRequest request);

        Task<IActionResult> GetConfirmationDetailsForApprovePublishProviderResults(HttpRequest request);

        Task<IActionResult> UpdatePublishedAllocationLineResultsStatus(HttpRequest request);

        Task<PublishedProviderResult> GetPublishedProviderResultByAllocationResultId(string allocationResultId, int? version = null);

        Task<PublishedProviderResultWithHistory> GetPublishedProviderResultWithHistoryByAllocationResultId(string allocationResultId);

        Task<IActionResult> ReIndexAllocationNotificationFeeds();

        Task FetchProviderProfile(Message message);

        Task MigrateVersionNumbers(Message message);

        Task MigrateFeedIndexId(Message message);

        Task<IActionResult> MigrateFeedIndexId(HttpRequest request);

        Task<IActionResult> MigrateVersionNumbers(HttpRequest request);

        PublishedAllocationLineResultVersion GetPublishedProviderResultVersionById(string id);

        PublishedProviderResult GetPublishedProviderResultByVersionId(string id);

        Task CreateAllocationLineResultStatusUpdateJobs(Message message);

        Task UpdateAllocationLineResultStatus(Message message);

        Task UpdateDeadLetteredJobLog(Message message);

        Task<IActionResult> MigratePublishedCalculationResults(HttpRequest request);

        Task MigratePublishedCalculationResults(Message message);

        Task MigrateInstructPublishedCalculationResults(Message message);
    }
}
