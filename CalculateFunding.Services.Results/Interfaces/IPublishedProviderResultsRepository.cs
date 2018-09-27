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

        Task<IEnumerable<PublishedProviderResult>> GetPublishedProviderResultsForSpecificationIdAndProviderId(string specificationId, IEnumerable<string> providerIds);

        Task<IEnumerable<PublishedProviderResult>> GetPublishedProviderResultsByFundingPeriodIdAndSpecificationIdAndFundingStreamId(string fundingPeriod, string specificationId, string fundingStreamId);

        Task<IEnumerable<PublishedProviderResult>> GetPublishedProviderResultsForSpecificationAndStatus(string specificationId, UpdatePublishedAllocationLineResultStatusModel filterCriteria);

        Task<IEnumerable<PublishedAllocationLineResultHistory>> GetPublishedProviderAllocationLineHistoryForSpecificationId(string specificationId);

        Task SavePublishedAllocationLineResultsHistory(IEnumerable<PublishedAllocationLineResultHistory> publishedResultsHistory);

        Task<PublishedAllocationLineResultHistory> GetPublishedProviderAllocationLineHistoryForSpecificationIdAndProviderId(string specificationId, string providerId, string allocationLineId);

        PublishedProviderResult GetPublishedProviderResultForId(string publishedProviderResultId);

        Task<PublishedProviderResult> GetPublishedProviderResultForId(string publishedProviderResultId, string providerId);


        Task<PublishedProviderResult> GetPublishedProviderResultForIdInPublishedState(string id);

        Task<PublishedAllocationLineResultHistory> GetPublishedAllocationLineResultHistoryForId(string id);

        Task<IEnumerable<PublishedProviderResult>> GetAllNonHeldPublishedProviderResults();
    }
}
