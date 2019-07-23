using CalculateFunding.Services.Publishing.Interfaces;
using Polly;

namespace CalculateFunding.Services.Publishing
{
    public class ResiliencePolicies : IPublishingResiliencePolicies
    {
        public Policy ResultsRepository { get; set; }

        public Policy SpecificationsRepositoryPolicy { get; set; }

        public Policy JobsApiClient { get; set; }
        
        public Policy PublishedFundingRepositoryPolicy { get; set; }

        public Policy PublishedFundingRepository { get; set; }

        public Policy PublishedProviderVersionRepository { get; set; }

        public Policy BlobClient { get; set; }
    }
}
