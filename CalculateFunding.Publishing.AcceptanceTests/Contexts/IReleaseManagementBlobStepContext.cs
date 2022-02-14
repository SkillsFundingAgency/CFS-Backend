using CalculateFunding.Publishing.AcceptanceTests.Repositories;

namespace CalculateFunding.Publishing.AcceptanceTests.Contexts
{
    public interface IReleaseManagementBlobStepContext
    {
        InMemoryBlobClient FundingGroupsClient { get; set; }

        InMemoryBlobClient ReleasedProvidersClient { get; set; }

        InMemoryAzureBlobClient PublishedProvidersClient { get; set; }

        InMemoryBlobClient PublishedFundingClient { get; set; }

    }
}