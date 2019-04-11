using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Models.Results;

namespace CalculateFunding.Services.Results.Interfaces
{
    public interface IPublishedProviderResultsRepository
    {
        Task SavePublishedResults(IEnumerable<PublishedProviderResult> publishedResults);

        Task<IEnumerable<PublishedProviderResult>> GetPublishedProviderResultsForSpecificationId(string specificationId);

        Task<IEnumerable<PublishedProviderResultExisting>> GetExistingPublishedProviderResultsForSpecificationId(string specificationId);

        Task<IEnumerable<PublishedProviderResultByAllocationLineViewModel>> GetPublishedProviderResultSummaryForSpecificationId(string specificationId);

        Task<IEnumerable<PublishedProviderResult>> GetPublishedProviderResultsForSpecificationIdAndProviderId(string specificationId, IEnumerable<string> providerIds);

        Task<IEnumerable<PublishedProviderResult>> GetPublishedProviderResultsByFundingPeriodIdAndSpecificationIdAndFundingStreamId(string fundingPeriod, string specificationId, string fundingStreamId);

        Task<IEnumerable<PublishedProviderProfileViewModel>> GetPublishedProviderProfileForProviderIdAndSpecificationIdAndFundingStreamId(string providerId, string specificationId, string fundingStreamId);

        Task<IEnumerable<PublishedProviderResultByAllocationLineViewModel>> GetPublishedProviderResultsSummaryByFundingPeriodIdAndSpecificationIdAndFundingStreamId(string fundingPeriodId, string specificationId, string fundingStreamId);

        Task<IEnumerable<PublishedProviderResult>> GetPublishedProviderResultsForSpecificationAndStatus(string specificationId, UpdatePublishedAllocationLineResultStatusModel filterCriteria);

        PublishedProviderResult GetPublishedProviderResultForId(string publishedProviderResultId);

        Task<PublishedProviderResult> GetPublishedProviderResultForId(string publishedProviderResultId, string providerId);

        Task<PublishedProviderResult> GetPublishedProviderResultForIdInPublishedState(string id);

        Task<IEnumerable<PublishedProviderResult>> GetAllNonHeldPublishedProviderResults();

        PublishedAllocationLineResultVersion GetPublishedProviderResultVersionForFeedIndexId(string feedIndexId);

        Task<IEnumerable<Migration.PublishedProviderResult>> GetPublishedProviderResultsForSpecificationIdAndProviderIdMigrationOnly(string specificationId, IEnumerable<string> providerIds);

        Task<IEnumerable<PublishedAllocationLineResultVersion>> GetAllNonHeldPublishedProviderResultVersions(string publishedProviderResultId, string providerId);
    }
}
