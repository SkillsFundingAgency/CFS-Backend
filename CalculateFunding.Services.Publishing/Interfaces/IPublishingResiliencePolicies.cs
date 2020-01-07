using CalculateFunding.Services.Core.Interfaces;
using Polly;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IPublishingResiliencePolicies : IJobHelperResiliencePolicies
    {
        Policy CalculationResultsRepository { get; set; }
        Policy SpecificationsRepositoryPolicy { get; set; }
        Policy JobsApiClient { get; set; }
        Policy ProvidersApiClient { get; set; }
        Policy PublishedProviderVersionRepository { get; set; }
        Policy PublishedFundingRepository { get; set; }
        Policy BlobClient { get; set; }
        Policy FundingFeedSearchRepository { get; set; }
        Policy PublishedProviderSearchRepository { get; set; }
        Policy PublishedFundingBlobRepository { get; set; }
        Policy CalculationsApiClient { get; set; }
        Policy PoliciesApiClient { get; set; }
        Policy SpecificationsApiClient { get; set; }
        Policy PublishedIndexSearchResiliencePolicy { get; set; }
    }
}
