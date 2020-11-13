using CalculateFunding.Services.DeadletterProcessor;
using Polly;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IPublishingResiliencePolicies
    {
        AsyncPolicy CalculationResultsRepository { get; set; }
        AsyncPolicy SpecificationsRepositoryPolicy { get; set; }
        AsyncPolicy ProvidersApiClient { get; set; }
        AsyncPolicy JobsApiClient { get; set; }
        AsyncPolicy PublishedProviderVersionRepository { get; set; }
        AsyncPolicy PublishedFundingRepository { get; set; }
        AsyncPolicy BlobClient { get; set; }
        AsyncPolicy FundingFeedSearchRepository { get; set; }
        AsyncPolicy PublishedProviderSearchRepository { get; set; }
        AsyncPolicy PublishedFundingBlobRepository { get; set; }
        AsyncPolicy CalculationsApiClient { get; set; }
        AsyncPolicy PoliciesApiClient { get; set; }
        AsyncPolicy ProfilingApiClient { get; set; }
        AsyncPolicy SpecificationsApiClient { get; set; }
        AsyncPolicy PublishedIndexSearchResiliencePolicy { get; set; }
        AsyncPolicy FundingStreamPaymentDatesRepository { get; set; }
        AsyncPolicy CacheProvider { get; set; }
    }
}
