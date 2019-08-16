using Polly;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IPublishingResiliencePolicies
    {
        Policy ResultsRepository { get; set; }
        Policy SpecificationsRepositoryPolicy { get; set; }
        Policy JobsApiClient { get; set; }
        Policy ProvidersApiClient { get; set; }
        Policy PublishedProviderVersionRepository { get; set; }
        Policy PublishedFundingRepository { get; set; }
        Policy BlobClient { get; set; }
        Policy FundingFeedSearchRepository { get; set; }
        Policy PublishedFundingBlobRepository { get; set; }
        Policy CalculationsApiClient { get; set; }
    }
}
