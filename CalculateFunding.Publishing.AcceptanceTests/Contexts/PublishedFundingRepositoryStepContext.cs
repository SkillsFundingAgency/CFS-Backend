using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Publishing.AcceptanceTests.Repositories;

namespace CalculateFunding.Publishing.AcceptanceTests.Contexts
{
    public class PublishedFundingRepositoryStepContext : IPublishedFundingRepositoryStepContext
    {
        public InMemoryPublishedFundingRepository Repo { get; set; }

        public PublishedProvider CurrentPublishedProvider { get; set; }

        public ICosmosRepository CosmosRepo { get; set; }

        public InMemoryBlobClient BlobRepo { get; set; }
    }
}
