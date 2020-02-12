using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.Storage;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Publishing.AcceptanceTests.Repositories;
using CalculateFunding.Services.Publishing.Interfaces;

namespace CalculateFunding.Publishing.AcceptanceTests.Contexts
{
    public class PublishedFundingRepositoryStepContext : IPublishedFundingRepositoryStepContext
    {
        private readonly IBlobClient _blobRepo;
        private readonly IPublishedFundingRepository _publishedFundingRepository;

        public PublishedFundingRepositoryStepContext(ICosmosRepository cosmosRepo,
            IBlobClient blobRepo,
            IPublishedFundingRepository publishedFundingRepository)
        {
            CosmosRepo = cosmosRepo;
            _blobRepo = blobRepo;
            _publishedFundingRepository = publishedFundingRepository;
        }

        public InMemoryPublishedFundingRepository Repo =>
            (InMemoryPublishedFundingRepository) _publishedFundingRepository;

        public PublishedProvider CurrentPublishedProvider { get; set; }

        public ICosmosRepository CosmosRepo { get; }

        public InMemoryBlobClient BlobRepo => (InMemoryBlobClient) _blobRepo;
    }
}
